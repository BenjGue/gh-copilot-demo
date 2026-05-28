# End-to-end tests (Playwright)

These tests drive the **deployed** album-viewer UI (Dokploy URL). The app
itself is never run locally — see the workshop notes in the repo root.

## Quick start

Pick one of the two modes below.

---

## Mode A — local Chromium (no admin required)

This is the recommended default for individual participants.

```powershell
cd album-viewer
npm install
npx playwright install chromium     # ~110 MB, downloads to %LocalAppData%\ms-playwright
npx playwright test
```

### Why no admin?

`npx playwright install chromium` downloads a portable Chromium build and
extracts it under your **user profile** (`%LocalAppData%\ms-playwright\` on
Windows, `~/.cache/ms-playwright` on macOS/Linux). It does **not** touch
`Program Files`, the Windows registry, or any system service, so no UAC
prompt and no admin token is needed.

What **does** need admin (and we deliberately avoid):

| Command | Why it needs admin |
|---|---|
| `npx playwright install chrome` | Runs the Google Chrome MSI installer system-wide |
| `npx playwright install msedge` | Same, for Microsoft Edge |
| `npx playwright install --with-deps` (on Linux) | Calls `apt-get install` for OS-level browser libs |

The test config uses `devices['Desktop Chrome']` (a viewport/UA preset),
which is fully satisfied by the bundled Chromium — no real Chrome required.

### If the chromium download fails

| Situation | What to do |
|---|---|
| Corporate proxy with TLS interception | `$env:HTTPS_PROXY = "http://proxy:port"` before running `install` |
| `%LocalAppData%` blocked by policy | Redirect: `$env:PLAYWRIGHT_BROWSERS_PATH = "C:\Users\you\pw-browsers"` then re-run install |
| Air-gapped / no internet | Use **Mode B** instead, or mirror the binaries internally with `PLAYWRIGHT_DOWNLOAD_HOST` |

---

## Mode B — shared remote browser (zero local install)

If your company hosts the **Playwright server** ([../playwright-server/](../../playwright-server/README.md)),
no browser download is needed at all. Just point the tests at the shared
WebSocket endpoint:

```powershell
cd album-viewer
npm install
$env:PLAYWRIGHT_WS_ENDPOINT = "wss://<your-playwright-server-domain>/ws"
$env:PLAYWRIGHT_BASE_URL    = "https://<your-album-viewer-domain>/"
npx playwright test
```

The config in [../playwright.config.ts](../playwright.config.ts) picks up
`PLAYWRIGHT_WS_ENDPOINT` and calls `chromium.connect(...)` under the hood,
so the browser runs server-side.

> **Version pinning matters.** The remote server and your local
> `@playwright/test` package must be on the **same** Playwright version
> (currently `1.48.0`). If you upgrade locally, ask DevOps to rebuild the
> server image.

---

## Targeting a different deployment

Both modes accept `PLAYWRIGHT_BASE_URL` to override the default URL baked
into the config:

```powershell
$env:PLAYWRIGHT_BASE_URL = "https://my-fork.traefik.me/"
npx playwright test
```

## Reports & artifacts

- HTML report: `npx playwright show-report` (opens the last run)
- Screenshots and traces (on failure) are saved under `test-results/`
- The cart test attaches `cart-with-first-album.png` to the report on success
