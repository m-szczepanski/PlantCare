# Implementation Plan

Step-by-step build order for the Plant Care App, derived from the milestones in `README.md`. Each step describes **what** to build, **how** to approach it, **how to test it**, and the **acceptance criteria** that must pass before moving on.

Work the steps strictly in order — each step depends on the artifacts of the previous ones. Keep each step (or a coherent slice of it) in its own scoped commit/PR so changes stay reviewable.

> Related docs: `overview.md`, `architecture.md`, `backend/docs/README.md`, `frontend/docs/README.md`.

## Testing Strategy (applies throughout)

| Level | Tooling | Scope |
|-------|---------|-------|
| Backend unit tests | xUnit + EF Core InMemory/SQLite in-memory | Services (due-date math, dedup), DTO mapping |
| Backend integration tests | `WebApplicationFactory` + SQLite test DB | Full endpoint behavior via HTTP |
| Frontend unit tests | Vitest + React Testing Library | Hooks, API client mocks, components |
| E2E smoke | Manual via Docker Compose (add Playwright later if needed) | Containers boot, happy-path user flow |

---

## Step 1 — Repo & Docker Scaffold

**What:** Finalize repo structure, `.env.example`, `docker-compose.yml`, Dockerfiles for `api` and `web`, and a health check endpoint.

**How:**
- Create `backend/PlantCare.Api` (.NET 10 Web API, minimal hosting in `Program.cs`) and remove the empty leftover `client/` and top-level `src/` scaffolding.
- `GET /health` returns `200 OK` with no DB dependency.
- Dockerfiles: multi-stage build; frontend image builds the Vite bundle and serves it via nginx with an `/api` reverse proxy to the `api` service.
- `docker-compose.yml` per the target shape in `README.md`; `.env.example` lists all env vars with comments.

**Test:**
- `docker compose up --build` from a clean checkout.
- `curl http://localhost:5001/health` → 200. `curl http://localhost:3000` → serves the SPA shell.

**Acceptance criteria:**
- [x] Clean clone + `docker compose up` boots all services without manual fixes.
- [x] `/health` responds 200 from the api container.
- [x] SPA loads in a browser at port 3000 and can reach the API through the nginx proxy.
- [x] `.env.example` documents every variable the compose file reads.

---

## Step 2 — EF Core Models + SQLite + Initial Migration

**What:** Entities (`Plant`, `PlantProfile`, `WateringLog`, `NotificationLog`), `AppDbContext`, SQLite provider, initial migration, seed loading from JSON.

**How:**
- Models in `Models/` exactly per the data model draft in `README.md` (nullable `PlantProfileId`, nullable `CustomWateringIntervalDays`).
- `AppDbContext` in `Data/`; connection string from configuration (SQLite file path under the `/data` volume).
- Seed JSON in `Seed/` with a handful of common species; loaded at startup (idempotent — skip rows that already exist).
- Generate the initial migration and commit it with the code.

**Test:**
- Backend integration test: boot the API against a temp SQLite file, verify tables exist and seed data was inserted (e.g., `GET /api/plant-profiles` later in step 3 — for now verify via context query in a unit test).

**Acceptance criteria:**
- [x] API starts and creates/opens the SQLite DB automatically; no manual migration step for end users.
- [x] Initial migration file is committed and applies cleanly to an empty DB.
- [x] Seed profiles are inserted idempotently (restarting twice doesn't duplicate rows).
- [x] `LightRequirement` enum stored consistently (defined storage conversion).

---

## Step 3 — Plant CRUD API + Minimal Frontend Views

**What:** Plants endpoints + plant list and detail views in the SPA.

**How:**
- Controller in `Controllers/` delegating to a `Services/` implementation; DTOs in `Dtos/` (never expose entities). The list response includes computed due status (`due today / overdue / days until due`) derived from interval logic — put that logic in a service, not the controller.
- Frontend: typed client in `src/api/`, data-fetching hooks in `src/hooks/` (TanStack Query), list + detail pages in `src/pages/` using shadcn components.

**Test:**
- Integration tests: CRUD round-trip (create → get → update → delete), 404 on unknown id, validation errors return 400.
- Frontend: component/hook tests with a mocked API client (renders plants, empty state).
- Manual: create a plant in the UI, see it in the list, open detail.

**Acceptance criteria:**
- [x] All plant CRUD endpoints behave per the API contract (including `400` on invalid payloads).
- [x] Due status appears in the list payload and is correct for edge cases (never watered, overdue, due today).
- [x] UI can create, view, edit, and delete a plant end-to-end.
- [x] API client is fully typed; no raw `fetch` in components.

**Deviations / notes:**
- The criteria said "six plant endpoints", but the `POST /api/plants/{id}/water` endpoint is explicitly Step 4's scope. Step 3 ships the five CRUD endpoints (`GET` list, `GET` by id, `POST`, `PUT`, `DELETE`); watering lands in Step 4.
- Added a minimal `GET /api/plant-profiles` read endpoint (Step 2's notes already anticipated it): the create/edit form needs the species list to populate the profile picker so a plant can inherit a default watering interval. Profile create/update (POST/PUT) remain Step 7 scope.
- Frontend tooling had to be introduced in this step (it didn't exist after Steps 1–2): React Router, TanStack Query, Tailwind CSS + shadcn base primitives, and the Vitest/React Testing Library harness.
- `WateringScheduleService` is the single source of truth for due-date math (custom interval → profile default; anchor = `LastWateredAt ?? AcquiredDate`), reused later by the dashboard (Step 5) and notification job (Step 6).


---

## Step 4 — Watering Log + "Mark as Watered"

**What:** `POST /api/plants/{id}/water` and the UI action.

**How:**
- Endpoint inserts a `WateringLog` row and updates `Plant.LastWateredAt` in one transaction. Optional note in the body.
- UI: "Mark as watered" button on plant detail (and ideally list row); invalidates the list/dashboard queries on success.

**Test:**
- Integration tests: watering updates `LastWateredAt`, appends a log row, returns 404 for unknown plant.
- Service unit test: due-status computation resets after watering.
- Manual: mark watered → due status disappears/reset in the UI.

**Acceptance criteria:**
- [ ] Watering updates both the log and `LastWateredAt` atomically.
- [ ] UI shows a watering history (from the log) on the detail page.
- [ ] Dashboard/list reflect the reset immediately after the action (no stale cache).

---

## Step 5 — Dashboard

**What:** `GET /api/dashboard` + dashboard UI.

**How:**
- Endpoint groups plants into `dueToday`, `overdue`, `upcoming` (reuse the same due-status service from step 3 — single source of truth).
- UI: dashboard as the default landing page with three sections using shared plant-card components.

**Test:**
- Service unit tests covering partition correctness (plant appears in exactly one bucket; empty buckets render fine).
- Integration test for the endpoint shape.
- Frontend component tests with seeded mock data.

**Acceptance criteria:**
- [ ] Every plant appears in exactly one of the three buckets; ordering within buckets is deterministic (e.g., most-overdue first).
- [ ] Dashboard is the default route and matches API output.
- [ ] Empty state (no plants) renders gracefully.

---

## Step 6 — Coravel Job + ntfy Notifications

**What:** Daily watering check, ntfy publishing, `NotificationLog` dedup.

**How:**
- Coravel scheduled job; cron from `WATERING_CHECK_CRON`. Inject the due-plants service (same one again) and the ntfy publisher.
- ntfy publisher: plain HTTP POST to `NTFY_URL`/topic from config; failures are logged and swallowed per-plant — one bad send must not abort the run.
- Dedup: before sending, check `NotificationLog` for a `WateringDue` send for that plant today.
- Add ntfy service to compose (already scaffolded in step 1).

**Test:**
- Unit tests: dedup logic (second run same day sends nothing), due filtering, partial-failure behavior.
- Integration: trigger the job manually (expose a dev-only trigger or make the service method public and test it directly) against a stub ntfy HTTP endpoint; assert POST bodies and log rows.
- Manual: run compose with ntfy, subscribe to the topic, force a plant overdue, trigger the check, verify push arrives.

**Acceptance criteria:**
- [ ] Job runs on the configured cron and is idempotent per day (no duplicate notifications).
- [ ] ntfy outage never crashes the job or blocks remaining plants; failure is logged.
- [ ] `NotificationLog` records every actual send with type and timestamp.
- [ ] A real push notification arrives on a subscribed device in the manual test.

---

## Step 7 — Care Tips Display

**What:** Show `PlantProfile` care tips (light, humidity, temperature, fertilizing) on the plant detail view.

**How:**
- Detail page renders the profile's tips (markdown-supported text) below plant info; behind the `ENABLE_CARE_TIPS` flag if set.
- Handle plants without a profile (no tips section, not an error).

**Test:**
- Frontend component tests: tips render from profile; hidden when no profile linked; flag off hides section.
- Manual: link a plant to a seeded profile with rich tips, verify formatting.

**Acceptance criteria:**
- [ ] Tips display for profiled plants and are absent (no crash/empty box) for unprofiled ones.
- [ ] `ENABLE_CARE_TIPS=false` hides the section without code changes.
- [ ] Profile edits (via plant-profiles endpoints) are reflected in the UI.

---

## Step 8 — Polish

**What:** Photo upload, empty states, shadcn theming pass, seed data expansion.

**How:**
- Photo: local-volume storage behind a static-file route from the API; `PhotoUrl` stores the served path. Keep upload size/type limits. (If the open question resolves to "skip photos for v1", ship without it and document that.)
- Expand `Seed/` JSON with a broader species set.
- Consistent theming via `tailwind.config.ts`; audit all views for spacing/typography/empty/loading states.

**Test:**
- Upload integration test (valid image, rejected types, size limit).
- Manual pass through every view with zero plants, one plant, and many plants.

**Acceptance criteria:**
- [ ] (If in scope) photo uploads persist across container restarts and render in list/detail.
- [ ] No view shows a blank screen in any state (loading, empty, error).
- [ ] Theme changes in `tailwind.config.ts` propagate across the whole app.
- [ ] Seed set covers the common species a home user would recognize.

---

## Definition of Done (every step)

- Code committed in a small scoped PR with tests for new behavior.
- `dotnet test` and frontend test suite green.
- `docker compose up --build` still boots clean from scratch.
- Docs updated if the step introduced a deviation from `README.md` / `architecture.md`.
