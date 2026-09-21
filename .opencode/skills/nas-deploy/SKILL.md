---
name: nas-deploy
description: Updates the PlantCare Docker stack on the user's NAS server by rebuilding and pushing GHCR images and guiding the Dockge pull-and-recreate. Use when the user asks to update/deploy/redeploy the app or the package on the NAS/server after changes were merged.
---

# PlantCare NAS Deployment

The NAS runs a Dockge-managed compose stack that pulls **published GHCR images only** — it never builds. Updating = rebuild + push images here, then the user clicks one button in Dockge. The agent has no access to the NAS; the final deploy step is always instructions for the user.

Use placeholders, never hard-code them into commits: `<owner>` = the GitHub account owning `ghcr.io/<owner>/plantcare-{api,web}`, `<nas>` = the server address, `<app-port>` = the mapped web port (as configured in the stack).

## 1. Sync and scope

1. `git fetch origin` and work from `main` (checkout + fast-forward pull). Images must always mirror `main`, never an unmerged branch.
2. Diff `main` against the last built commit (ask the user or compare against the previously mentioned digest commit) to decide what to rebuild:
   - `backend/**` changed → rebuild **api**
   - `frontend/**` (or `frontend/nginx.conf`) changed → rebuild **web**
   - unsure → rebuild both (cheap enough).
3. Verify before shipping: `dotnet test backend/PlantCare.Api.Tests` green, `npm run build && npm run test` green in `frontend/`.

## 2. Registry auth (this machine)

```bash
gh auth status                 # must show github.com as <owner>
gh cr login 2>/dev/null || gh auth token | docker login ghcr.io -u <owner> --password-stdin
```

If a `docker push` gets `denied: denied`, the token lacks `write:packages`:

```bash
gh auth refresh -h github.com -s write:packages   # device-code flow, tell the user the code immediately — it expires in ~15 min
```

## 3. Build & push

The NAS is **amd64**; development Macs are often arm64. Always pass the platform explicitly:

```bash
docker buildx build --platform linux/amd64 -t ghcr.io/<owner>/plantcare-api:latest --push backend/PlantCare.Api
docker buildx build --platform linux/amd64 -t ghcr.io/<owner>/plantcare-web:latest --push frontend
```

- Contexts are the service directories (same as `docker-compose.yml`), run from the repo root.
- Report the pushed manifest digests to the user — the Dockge UI should show a changed digest after pull.
- Packages are public (pulled without credentials on the NAS). If a new package was created, remind the user to flip its visibility in GitHub package settings.

## 4. Deploy on the NAS (user action)

Instruct the user: Dockge UI → the `plantcare` stack → **Pull images and Recreate**. Then verification steps for them:

1. api container logs show `Applying migration ...` lines for any new EF migrations (migrations run automatically at startup — never apply them manually).
2. The app is reachable at `http://<nas>:<app-port>`.
3. Data safety: all persistent state lives in the named volumes (`plant-data`, `plant-photos`, `ntfy-data`) and survives recreate. **Never suggest `docker compose down -v`** outside of a deliberate factory reset (the first-run setup wizard only appears on a wiped DB — mention `-v` only in that context).

## 5. Known failure modes

| Symptom | Cause / fix |
|---------|-------------|
| `denied: denied` on login/push | Missing `write:packages` scope → step 2 refresh |
| `Bind for 0.0.0.0:PORT failed: port is already allocated` | Another app owns the host port → user edits the stack's `ports:` mapping in Dockge (internal container ports must stay: web 80, api 8080, ntfy 80). Note `<app-port>` may change, and `QUICK_ACTION_URL_BASE` in the stack env must follow it |
| Pull says everything is up to date after a push | The image for that service was not rebuilt (wrong digest) or the push failed silently — rebuild and confirm the new digest in the build output |
| Photo/upload requests fail with 413 | nginx `client_max_body_size` in `frontend/nginx.conf` below the API's limits — a frontend change, rebuild web |
| App loads but API calls 502 | api container restarting — check its logs for failed migrations or DB connection env issues |

## 6. Housekeeping

- If the changes were deployed from a still-open branch (should not happen per §1), remind the user to merge so `main` matches the registry.
- No secrets (tokens, passwords, the NAS address) ever enter the repo, docs, or commit messages.
