# Running the GitHub Copilot Workshop with Dokploy

> Adaptation of the [Ultimate GitHub Copilot Hands-on Lab](https://aka.ms/github-copilot-hol) for a setup where **nothing runs on the participant's laptop**. Each participant only uses VS Code + GitHub Copilot + their own GitHub fork. Every `git push` is auto-built and auto-deployed by a self-hosted [Dokploy](https://dokploy.com) server, which exposes the web UI on a generated URL.
>
> **Scope: Levels 3, 4 and 5 only.** Levels 1, 2 and 6 are out of scope.

---

## 1. Overview & goals

| Topic | Original workshop | This adaptation |
|---|---|---|
| Where code runs | Local laptop or GitHub Codespaces | Dokploy server (Azure VM) |
| Local tooling needed | Node, .NET, Docker, browsers | **None** beyond VS Code + Copilot + Git |
| How participants see their changes | `npm run dev` / `dotnet run` on localhost | Auto-deployed URL (`*.traefik.me`) after every push |
| Levels covered | 1 → 6 | **3 → 5 only** |
| Build & deploy | Manual | Automatic on push (Dokploy webhook) |

The goal is to let each participant focus purely on **prompting Copilot, pushing code, and observing the deployed result** — no local toolchain to install or maintain.

---

## 2. Pre-requisites

### 2.1 For each participant
- A GitHub account.
- **GitHub Copilot Pro or Business** license (paid). Level 4 uses the *Copilot Code Review* agent on PRs, which is not in the Free tier.
- [Visual Studio Code](https://code.visualstudio.com/) with:
  - `GitHub.copilot` extension
  - `GitHub.copilot-chat` extension
- Git installed and signed in to GitHub.
- A modern web browser.
- **No Node.js, no .NET SDK, no Docker** required on the laptop.

### 2.2 For the organizer (you)
- An Azure subscription with rights to create a Resource Group and a VM.
- Owner/admin rights on the GitHub fork that participants will fork from.
- (Optional) A GitHub OAuth App or a Personal Access Token for the Dokploy ↔ GitHub integration.

---

## 3. Architecture

```
[ Participant laptop ]              [ GitHub.com ]                  [ Azure ]
 VS Code + Copilot   ── git push ─►  fork of repo   ── webhook ──►  Dokploy VM
                                       │                              │
                                       │                              ├─► build images (docker compose)
                                       │                              ├─► run containers behind Traefik
                                       │                              └─► expose <app>-<id>.traefik.me
                                       ▼
                                Copilot Code Review / Issues / PR
```

---

## 4. Provision Dokploy on Azure (pre-test sizing: 1 user)

> Sizing chosen for your **personal pre-test**: `Standard_B2s` (2 vCPU / 4 GB / 30 GB SSD). See [§9 Multi-user scaling](#9-scaling-for-the-real-workshop) for the production sizing.

### 4.0 Fast path: deploy with the bundled Bicep template (recommended)

The repo ships [iac/bicep/dokploy-vm.bicep](iac/bicep/dokploy-vm.bicep), which creates the VM + NSG + public IP **and** runs the Dokploy installer via cloud-init on first boot. One command:

```powershell
$rg = "rg-dokploy-workshop"
az group create -n $rg -l westeurope

az deployment group create `
  -g $rg `
  -f iac/bicep/dokploy-vm.bicep `
  -p adminUsername=azureuser sshPublicKey="$(Get-Content $HOME/.ssh/id_rsa.pub -Raw)"
```

The deployment outputs `dokployUrl` — wait ~3–5 minutes after the deployment completes for cloud-init to finish installing Docker + Dokploy, then open that URL in your browser and skip to [§4.4](#44-first-login).

If you prefer to do it step by step (or already have a VM), use the manual instructions below.

### 4.1 Create the resource group and VM

```powershell
$rg       = "rg-dokploy-workshop"
$location = "westeurope"
$vmName   = "vm-dokploy"
$adminUser = "azureuser"

az group create -n $rg -l $location

az vm create `
  --resource-group $rg `
  --name $vmName `
  --image Ubuntu2404 `
  --size Standard_B2s `
  --admin-username $adminUser `
  --generate-ssh-keys `
  --public-ip-sku Standard `
  --os-disk-size-gb 30
```

### 4.2 Open the required ports

Dokploy needs **22** (SSH), **80** (HTTP/Traefik), **443** (HTTPS/Traefik) and **3000** (Dokploy UI).

```powershell
az vm open-port -g $rg -n $vmName --port 22   --priority 1001
az vm open-port -g $rg -n $vmName --port 80   --priority 1002
az vm open-port -g $rg -n $vmName --port 443  --priority 1003
az vm open-port -g $rg -n $vmName --port 3000 --priority 1004
```

> ⚠️ Make sure ports 80/443/3000 are not already in use on the VM — Dokploy installation fails otherwise.

### 4.3 Install Dokploy

```powershell
$ip = az vm show -d -g $rg -n $vmName --query publicIps -o tsv
ssh "$adminUser@$ip"
```

Then on the VM:

```bash
curl -sSL https://dokploy.com/install.sh | sh
```

The script installs Docker if needed, sets up Docker Swarm, deploys Traefik, and starts the Dokploy UI on port 3000.

### 4.4 First login

Open `http://<vm-public-ip>:3000` in your browser, create the admin account, and you land on the Dokploy dashboard.

---

## 5. Prepare the workshop fork

Since the application must build on Dokploy's server (no local run), the repo needs a deterministic build definition.

### 5.1 Containerization artifacts (committed in this repo)

The following files are already present in the repo and used by Dokploy:

```
.
├── docker-compose.yml             # orchestrates api + viewer
├── albums-api/
│   └── Dockerfile                 # multi-stage: dotnet/sdk:8.0 → aspnet:8.0
└── album-viewer/
    ├── Dockerfile                 # multi-stage: node:20 build → nginx:alpine serve
    └── nginx.conf                 # SPA fallback + reverse-proxy /albums → albums-api:3000
```

Key design choices:
- `albums-api` listens on port **3000** (set via `ASPNETCORE_URLS=http://+:3000`); HTTPS is terminated by Traefik in front of the container.
- `album-viewer` is built once with `vite build` and served as static assets by nginx on port 80.
- The viewer's nginx config **reverse-proxies `/albums` to `http://albums-api:3000`** using the Docker Compose service name. This means:
  - No CORS configuration needed in the browser.
  - **No `VITE_ALBUM_API_HOST` env var to bake at build time** — the API URL is never exposed to the browser.
  - The viewer image is fully portable: same image works regardless of the public hostname Dokploy assigns.

### 5.2 Publish your fork

1. Fork the upstream repo to your GitHub account (organizer fork).
2. Add the Docker artifacts above, commit, push to `main`.
3. Each participant then forks **your** fork (not the original).

---

## 6. Wire the repo to Dokploy

### 6.1 Connect GitHub to Dokploy

In Dokploy UI → `Settings` → `Git Sources` → add a **GitHub** source (OAuth App is simplest; PAT also works). Authorize Dokploy to access your fork.

### 6.2 Create the Project and Compose application

1. `Projects` → `Create Project` → name it `copilot-workshop`.
2. Inside the project → `Create Service` → **Compose**.
3. Source = your GitHub fork, branch = `main`.
4. Compose file path = `docker-compose.yml`.
5. **Environment variables**: none required. The viewer reaches the API through the internal Docker network via the nginx reverse-proxy.
6. **Domains** tab → generate a free `*.traefik.me` domain for the **`album-viewer`** service (port 80). Enable HTTPS (auto Let's Encrypt via Traefik).
   - The `albums-api` service does **not** need a public domain — it is only reached internally. Expose it publicly only if you want to demo Swagger directly (assign a second domain pointing at port 3000).

### 6.3 Enable auto-deploy

On the application's `Deployments` tab, copy the **webhook URL** and add it to the GitHub repo: `Settings` → `Webhooks` → `Add webhook` → content type `application/json` → event = `push`. Dokploy can also create this webhook automatically when the GitHub source is OAuth-based.

### 6.4 First deploy

Click `Deploy`. Watch the build log. Once green, open the viewer's generated URL.

---

## 7. Validate auto-deploy

1. In your local VS Code, edit `README.md` (trivial change).
2. `git commit && git push`.
3. Dokploy `Deployments` tab shows a new build starting within seconds.
4. After build finishes, refresh the viewer URL — change visible (if visible in UI).

If this works, the workshop pipeline is ready.

---

## 8. Running Levels 3 → 5 in adapted mode

Below, every step of the upstream workshop is marked **unchanged** (works as-is) or **adapted: …** (something to change in the participant's instructions).

### Level 3 — Copilot Agent
| Step | Status |
|---|---|
| Code Generation (add CRUD routes) | **unchanged** — pushes trigger rebuild on Dokploy; verify via API URL `/swagger`. |
| Code Refactoring (Artist model) | **unchanged**. |
| Tests Generation (unit tests) | **adapted:** tests run during the Docker build (`dotnet test` step in the Dockerfile) instead of locally. Failed tests = failed deploy. |

### Level 4 — Plan & Implement
| Step | Status |
|---|---|
| Step 1 — Plan agent rewrite of the API to NodeJS (`album-api-v2`) | **adapted:** the Copilot prompt asks for the new API to run on port 3000 — keep that. After the rewrite, **update `docker-compose.yml`** to point the `albums-api` service at the new folder/Dockerfile. Ask Copilot to also generate the new Dockerfile in the same session. |
| Step 2 — Agent implements the plan | **unchanged**. |
| Step 3 — Multi-language support | **unchanged**. |
| Step 4 — Setup MCP servers | **adapted:** use the **remote HTTP GitHub MCP** (`https://api.githubcopilot.com/mcp/` in `.vscode/mcp.json`) — no local Docker needed. Playwright MCP runs locally in VS Code via npx (still no app running locally, only the headless browser). |
| Step 5 — Create an issue via GitHub MCP | **unchanged**. |
| Step 6 — Implement the cart feature | **unchanged**; push the feature branch and let Dokploy build a preview. |
| **Step 7 — Playwright tests** | **adapted:** replace the prompt's `http://localhost:3001` with the participant's Dokploy URL for the viewer (e.g. `https://album-viewer-xyz.traefik.me`). The participant must push their branch first so Dokploy has deployed it. |
| Step 6 (bis) — Code Review on PR | **unchanged** (requires paid Copilot). |

### Level 5 — Advanced Copilot Concepts
All sub-sections — Prompt Engineering, `copilot-instructions.md`, split instructions, reusable prompts, custom agents, fetch web pages, vision — are **purely local VS Code / Copilot operations** and require **no change**.

---

## 9. Scaling for the real workshop

The pre-test VM (`B2s`) is fine for **one** user. For a real session with N participants the recommended option is:

- **Option A (recommended): one Dokploy VM per participant**, deployed from a Bicep template participants run themselves. Clean isolation, predictable cost (~€10/user/day for B2s), participants can keep their VM after the workshop.
- **Option B: a single beefier shared Dokploy** (`Standard_D4s_v5` or `D8s_v5`), one Dokploy *Project* per participant. Cheaper but builds compete for CPU/RAM during simultaneous pushes — noisy-neighbor risk during the live session.

Either way, document the chosen path in your participant invite email.

---

## 10. Troubleshooting

| Symptom | Likely cause | Fix |
|---|---|---|
| Dokploy install fails on `port already in use` | NGINX/Apache pre-installed on the VM image | `sudo systemctl stop nginx apache2 && sudo systemctl disable nginx apache2` then re-run installer. |
| Build OOM on B2s | 4 GB not enough for parallel api + viewer builds | Add a 4 GB swap file (`fallocate -l 4G /swapfile …`) or move to `B2ms` / `D2s_v5`. |
| GitHub webhook 401/403 | OAuth scopes too narrow / PAT expired | Re-authorize the GitHub source in Dokploy `Settings`. |
| `traefik.me` TLS error | Let's Encrypt rate limit on first deploys | Wait 10 min, redeploy; or switch to HTTP-only for the test. |
| Viewer loads but API calls 404 | nginx reverse-proxy not reaching the API container | Check both services are `running` in Dokploy; confirm the API service is named `albums-api` in `docker-compose.yml` (must match `proxy_pass` in `album-viewer/nginx.conf`). |
| Container restart loop | Wrong port exposed | Check `EXPOSE` in Dockerfile matches the `port` set in Dokploy → Domains. |

---

## 11. Cleanup

```powershell
az group delete -n rg-dokploy-workshop --yes --no-wait
```

---

## 12. Status of implementation artifacts

| Artifact | Status | Path |
|---|---|---|
| .NET 8 API Dockerfile | ✅ done | [albums-api/Dockerfile](albums-api/Dockerfile) |
| Vue viewer Dockerfile | ✅ done | [album-viewer/Dockerfile](album-viewer/Dockerfile) |
| nginx reverse-proxy config | ✅ done | [album-viewer/nginx.conf](album-viewer/nginx.conf) |
| Root Compose file | ✅ done | [docker-compose.yml](docker-compose.yml) |
| Workshop-mode Copilot instructions | ✅ done | [.github/copilot-instructions.md](.github/copilot-instructions.md) |
| Bicep template for participant VMs (Option A scaling) | ✅ done | [iac/bicep/dokploy-vm.bicep](iac/bicep/dokploy-vm.bicep) |
