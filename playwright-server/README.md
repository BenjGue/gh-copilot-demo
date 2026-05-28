# Playwright Server (shared remote browser for workshop participants)

A self-contained Docker app that hosts a **remote Playwright browser server**.
Participants run tests from their laptop, but the browser actually runs inside
this container. **No local browser install is required on the participant's
machine** — only `@playwright/test` as an npm dependency.

This folder is **independent** from `album-viewer/`. Your company's DevOps
deploys it once into your own Dokploy (or any Docker host), and shares the
public URL with all workshop participants.

---

## 1. What it is

| | |
|---|---|
| Base image | `mcr.microsoft.com/playwright:v1.48.0-noble` (Chromium + Firefox + WebKit preinstalled) |
| Exposed port | `5400` |
| Endpoint path | `/ws` (WebSocket) |
| Final endpoint | `wss://<your-domain>/ws` |
| Auth | **None by default** — protect via Traefik basic-auth or a private network |

The container runs `npx playwright run-server --port 5400 --path /ws`. Each
client connection gets an isolated browser context.

---

## 2. Deploy to Dokploy (DevOps task — done once)

1. In Dokploy → **Create application** → type **Docker Compose**.
2. Source = this repo (or a fork), branch = `main`, path = `playwright-server`.
3. Save. Click **Deploy**. First build pulls the Playwright image (~1.5 GB)
   and takes a few minutes.
4. Open the **Domains** tab → generate a `*.traefik.me` domain for the
   `playwright-server` service (port **5400**). Enable HTTPS.
5. Verify the WebSocket endpoint:
   ```bash
   curl -i -N \
     -H "Connection: Upgrade" -H "Upgrade: websocket" \
     -H "Sec-WebSocket-Key: dGVzdA==" -H "Sec-WebSocket-Version: 13" \
     https://<your-domain>/ws
   ```
   Expected: HTTP `101 Switching Protocols`.

### Update procedure

When you bump `@playwright/test` in `album-viewer/package.json`, also bump
`PLAYWRIGHT_VERSION` in:
- `Dockerfile` (ARG default)
- `docker-compose.yml` (build args + image tag)

Playwright **requires the client and server versions to match exactly**.

### Securing it (recommended)

Pick one:

- **Basic auth via Traefik** — add the labels to the compose service:
  ```yaml
  labels:
    - traefik.http.middlewares.pw-auth.basicauth.users=user:$$apr1$$...
    - traefik.http.routers.playwright-server.middlewares=pw-auth
  ```
- **IP allow-list** — restrict in Traefik or the VM's NSG.
- **Private network only** — don't expose a public domain; only reachable
  from participants on a VPN.

---

## 3. Use from the test project (participant task)

In [album-viewer/](../album-viewer), the Playwright config already supports a
remote endpoint via the `PLAYWRIGHT_WS_ENDPOINT` env var.

```powershell
cd album-viewer
npm install                                 # installs @playwright/test, no browsers needed
$env:PLAYWRIGHT_WS_ENDPOINT = "wss://<your-playwright-server-domain>/ws"
$env:PLAYWRIGHT_BASE_URL    = "https://<your-album-viewer-domain>/"
npx playwright test
```

With `PLAYWRIGHT_WS_ENDPOINT` set, Playwright skips downloading any browser
binary and connects to the shared server instead.

If you provide HTTP basic-auth, put creds in the URL: `wss://user:pass@host/ws`.

---

## 4. Local alternative (no shared server)

If you'd rather have each participant run a browser locally, they can install
just Chromium (no admin required — see
[../album-viewer/tests/README.md](../album-viewer/tests/README.md)).

---

## 5. Troubleshooting

| Symptom | Cause / fix |
|---|---|
| `browserType.connect: WebSocket error: 404` | Wrong path. Endpoint must be `/ws`. |
| `Protocol error … Target closed` immediately | Client/server Playwright versions differ. Bump both to the same. |
| Hangs at `connect` | Traefik not upgrading WebSocket. Confirm `wss://` (not `https://`) and that you reached step 2.5 above with a `101`. |
| OOM kills in container | Increase `deploy.resources.limits.memory` in compose; default 2 GB is enough for ~5 concurrent contexts. |
| `Failed to launch chromium because executable doesn't exist` (server logs) | Image was built against a different Playwright version than the `npx playwright run-server` invocation. Rebuild after bumping `PLAYWRIGHT_VERSION`. |
