# syntax=docker/dockerfile:1.7
#
# Playwright remote browser server.
#
# Exposes a WebSocket endpoint that test runners connect to via
# `chromium.connect('ws://<host>:5400/ws')`. No browsers need to be installed
# on the participant's machine.
#
# Image already ships Chromium + Firefox + WebKit + all OS deps preinstalled.
# Pin the version to the same Playwright version used by the test project
# (see album-viewer/package.json -> @playwright/test).

ARG PLAYWRIGHT_VERSION=1.48.0
FROM mcr.microsoft.com/playwright:v${PLAYWRIGHT_VERSION}-noble

# Re-declare ARG after FROM so it's available in this stage.
ARG PLAYWRIGHT_VERSION=1.48.0
ENV PLAYWRIGHT_VERSION=${PLAYWRIGHT_VERSION}

WORKDIR /srv

# Healthcheck: TCP port is open.
HEALTHCHECK --interval=30s --timeout=5s --start-period=10s --retries=3 \
  CMD bash -c "</dev/tcp/127.0.0.1/5400" || exit 1

EXPOSE 5400

# `run-server` is the official Playwright command for hosting a shared
# browser backend. `--path /ws` makes the WebSocket endpoint deterministic.
CMD ["sh", "-c", "npx -y playwright@${PLAYWRIGHT_VERSION} run-server --port 5400 --path /ws"]
