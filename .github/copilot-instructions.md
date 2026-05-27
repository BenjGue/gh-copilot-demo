# Workshop-mode Copilot instructions
#
# This repository is being used in a workshop where the application is **never
# run locally**. All builds and deployments happen on a remote Dokploy server,
# triggered automatically by `git push`. The deployed UI is reachable through a
# generated `*.traefik.me` URL.
#
# When you (Copilot) propose changes, follow these rules:
#
# 1. **Do not suggest running the app locally.**
#    Avoid proposing `npm run dev`, `dotnet run`, `dotnet watch`, `vite`,
#    `docker compose up`, etc., as the validation path. The participant will
#    `git commit && git push` and observe the result on the Dokploy URL.
#
# 2. **Preserve the container contract.**
#    - `albums-api` must listen on port **3000** (set via `ASPNETCORE_URLS`).
#    - `album-viewer` is built as static assets and served by nginx on port 80.
#    - The nginx reverse-proxy forwards `/albums` to `http://albums-api:3000`.
#      Keep this contract intact unless explicitly asked to change it.
#
# 3. **If you rewrite a service (e.g. Level 4 NodeJS rewrite of the API)**,
#    also update:
#      - the corresponding `Dockerfile`
#      - `docker-compose.yml` (service name `albums-api` must keep port 3000
#        and remain reachable from the viewer's nginx reverse-proxy)
#    in the same change set, so the next `git push` produces a working deploy.
#
# 4. **Tests run inside the Docker build, not locally.**
#    When generating tests, also wire them into the relevant `Dockerfile`
#    build stage (e.g. an extra `RUN dotnet test` or `RUN npm test`) so a
#    failing test fails the deploy.
#
# 5. **Browser-side URLs must remain relative.**
#    The viewer calls `/albums` (relative). Do not switch to absolute URLs
#    pointing at `localhost` or the API's `traefik.me` hostname — the nginx
#    reverse-proxy handles routing transparently.
#
# 6. **For Playwright tests (Level 4 Step 7)**, target the deployed viewer URL
#    provided by the participant, not `http://localhost:3001`.
