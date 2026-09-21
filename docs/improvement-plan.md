# Improvement Plan (post-v1)

Deliverable chunks derived from `ideas.md`. Each chunk is one scoped commit group inside an **epic branch/PR** (repo convention since Epic 3: one PR per epic, `epic/<name>` off main — chunks stay the unit of work and acceptance, the epic is the unit of review and shipping). Chunks remain sized to be shippable without breaking v1. This is a proposal — reorder freely; only the listed dependencies are hard.

Every chunk follows the AGENTS.md workflow: acceptance criteria defined before coding, builds green (`dotnet build`, `npm run build`), tests for new behavior, migration with any schema change, docs updated.

## Epic 1 — App Shell & Theming (frontend-only, no dependencies)

| Branch | Deliverable | Covers idea |
|--------|-------------|-------------|
| `feature/theme-toggle` | Theme provider (light/dark/system), persisted preference, toggle control; dark tokens already in `index.css` | UI: theme toggle |
| `feature/app-shell` | Persistent header/sidebar with nav (Dashboard, Plants, later rooms/settings), holds the theme toggle | UI: app shell |
| `feature/feedback-toasts` | shadcn toast system wired into all mutation hooks (success + failure) | UX: toast system |
| `feature/loading-empty-states` | Card-shaped skeletons; consistent empty states across dashboard/list/detail | UI: skeletons, empty states |
| `feature/pwa-shell` | Favicon, PWA manifest, icons, installable on phone home screen | UI: PWA |

## Epic 2 — Plant Look & Photos

| Branch | Deliverable | Covers idea |
|--------|-------------|-------------|
| `feature/plant-thumbnails` | Render `PhotoUrl` as card/detail thumbnails + placeholder illustration when empty | UI: plant photos |
| `feature/photo-upload` | Upload endpoint storing under `/data` volume, served via nginx; replaces URL-only flow (needs storage decision) | UI: photo upload |
| `feature/plant-card-redesign` | Days-until-due as primary number, due-soon badge treatment, relative "watered N days ago", accessible due status (text/icon, not color alone) | UI: card polish; UX: accessible due status |
| `feature/care-tips-markdown` | Styled markdown for care tips (headings, lists, optional TOC for long tips) | UI: markdown rendering tweaks |
| `feature/mobile-layout-pass` | Phone-width audit of dashboard/list/detail, touch targets | UI: mobile layout |

## Epic 3 — Watering & Care UX

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/optimistic-watering` | Water button on dashboard cards, optimistic update + undo toast, confirm dialog on delete | feedback-toasts | UX: optimistic watering, inline water, delete confirm |
| `feature/plant-form-ux` | Combobox profile picker with search, live "next due" preview, sensible defaults, inline 400 validation | — | UX: form UX, validation feedback |
| `feature/watering-history-viz` | Watering history chart on plant detail (dots / bars per month) from the existing logs endpoint | — | UI: watering history visualization |
| `feature/plant-search-filters` | Search + filter/sort on the plants list (name, location, due status, species) | — | Func: search/filter/sort |
| `feature/keyboard-shortcuts` | `n` new plant, `w` water focused card, `/` focus search | plant-search-filters | UX: keyboard shortcuts |
| `feature/routing-polish` | Breadcrumbs, deep-link/shareable detail URLs, scroll + tab/filter restore | — | UX: deep links, memory |

## Epic 4 — Dashboard Views

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/dashboard-stats` | Stat strip (due / overdue / total) with green/empty states | — | UI: stat strip |
| `feature/calendar-view` | Upcoming-care week/month grid from schedule data | — | UI: calendar view |
| `feature/ical-feed` | `GET /api/calendar.ics` generated from due dates; nginx path | calendar-view (shared computation) | Func: iCal feed |
| `feature/wall-mode` | Read-only auto-refreshing full-screen route for a home tablet | dashboard-stats | UI: wall mode |
| `feature/collection-insights` | Read-only stats page/endpoint: totals, species diversity, most-neglected plants, adherence/streaks, seasonal trends | care-task-model (if task types included) | Func: collection insights |

## Epic 5 — Rooms (data model + light matching)

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/room-entity` | `Room` entity (name, orientation enum), migration, plant FK replacing free-text `Location` (with migration of existing values), room CRUD + picker in plant form, group-by-room dashboard view | — | Func: rooms; UI: group by location |
| `feature/room-environment` | Optional room params (light exposure, humidity, temperature), per-room view, `LightRequirement`-vs-room matching: recommendations + "wrong room" flags | room-entity | Func: orientation/env + matching |
| `feature/room-sensors` | Feed room humidity/temperature from an existing smart-home source (e.g. MQTT/HA); design decision needed first | room-environment | Func: sensor-fed room values |

## Epic 6 — Care Tasks & Scheduling

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/care-task-model` | Generalize watering into typed `CareTask` + interval + log (migration, keep water endpoint working), "mark as done" per type | — | Func: care task types |
| `feature/fertilizing-schedule` | Fertilizing as a scheduled task type with season/flush reminders | care-task-model | Func: fertilizing |
| `feature/seasonal-intervals` | Per-plant/profile "reduce in winter" interval adjustment (revisit v1 non-goal explicitly) | care-task-model | Func: seasonal adjustments |
| `feature/repot-lifecycle` | Pot size, soil mix, last/next repot dates, propagation + owned-since display | care-task-model (repot task) | Func: repot lifecycle, propagation dates |
| `feature/water-details` | Optional amount/method (tap/filtered/rainwater) on the watering log | — | Func: water details |
| `feature/plant-notes` | Free-text health notes/status per plant (list + create endpoint + detail section) | — | Func: plant notes |

## Epic 7 — Notifications

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/digest-notifications` | One daily digest ntfy message + overdue escalation priority + per-plant notification on/off | — | Func: due notification improvements |
| `feature/vacation-snooze` | Snooze a plant (or all plants) for N days; dedup respects it | digest-notifications | Func: on-vacation |
| `feature/ntfy-quick-actions` | Shared-secret trigger URL to water-from-notification; rate-limited, logged | — | Func: quick actions |
| `feature/extra-channels` | Optional email/Telegram publishers behind `INtfyPublisher`-style abstraction, env-gated | — | Func: additional channels |

## Epic 8 — Species Knowledge

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/profile-management-ui` | CRUD UI over the existing plant-profiles endpoints (care tips, defaults) | plant-form-ux (combobox reuse) | UI: profile management |
| `feature/seed-expansion` | More species + curated tips; drop-in JSON folder watched at startup for user species | — | Func: seed expansion |
| `feature/species-catalog-search` | Type-ahead species search in the plant form backed by an external catalog (provider decision: data quality, rate limits, offline); import/cache matches as local profiles | profile-management-ui | Func: wider catalog |
| `feature/toxicity-flags` | `ToxicToPets`/`ToxicToChildren` on profile (migration), badges on cards/detail, mention in notification text | — | Func: toxicity awareness |
| `feature/diagnostics-checklist` | Per-profile symptom → cause checklist rendered on detail; vision-model photo diagnosis stays an experiments doc, not built | — | Func: diagnostics |
| `feature/care-journal` | Dated photo+note entries per plant (new entity + upload dependency), before/after view on detail | photo-upload | Func: care journal |

## Epic 9 — Platform & Data Safety

| Branch | Deliverable | Depends on | Covers idea |
|--------|-------------|------------|-------------|
| `feature/bulk-water` | "Water all" per dashboard bucket / per room; batch endpoint or client-side loop with one invalidation | room-entity (room grouping) | Func: bulk actions |
| `feature/data-export-import` | JSON export/import of plants, rooms, profiles, logs; backup docs in README | — | Func: export/import |
| `feature/openapi` | Swashbuckle/OpenAPI JSON exposed for power users | — | Func: OpenAPI |
| `feature/app-status` | Status endpoint + in-app page: last job run, ntfy reachability, scheduler health (uses reserved `healthApi`) | — | UX: status page |
| `feature/timezone-config` | `TZ` env for the cron, documented; consistent local-time display | — | UX: timezone awareness |
| `feature/onboarding` | First-run dashboard: add-plant prompt + ntfy subscribe link/topic from config | app-status (config echo) | UX: onboarding |
| `feature/postgres-profile` | Optional Postgres compose profile tested + documented | — | Func: Postgres |

## Progress

- `feature/theme-toggle` — done. `ThemeProvider` + `useTheme` (light/dark/system, persisted in `localStorage`, system-following), `ModeToggle` dropdown in the header, pre-paint FOUC script in `index.html`, Vitest coverage; dark tokens unchanged in `index.css`.
- `feature/app-shell` — done. shadcn `sidebar`-based `AppShell` (brand + Dashboard/Plants/Add plant nav with route-driven active state, mobile off-canvas with sidebar trigger, theme toggle in the sticky top bar); `App.tsx` layout now delegates to it.
- `feature/feedback-toasts` — done. `sonner` `<Toaster />` mounted in `main.tsx`; create/update/delete/water hooks emit success + error toasts via `src/lib/toast.ts` (API `detail` preferred for 400s); removed the ad-hoc "Could not log watering" paragraph. Deviation: the generated `ui/sonner.tsx` was rewritten to use our `ThemeProvider` instead of next-themes; the plant form keeps its inline error banner on top of the failure toast (inline field errors land in `feature/plant-form-ux`).
- `feature/loading-empty-states` — done. Card-shaped `PlantCardSkeletonGrid`/`PlantDetailSkeleton` (sr-only `role="status"` labels) replace "Loading…" text on dashboard/list/detail; shared `EmptyState` used by `NoPlantsEmptyState` (dashboard + list), the watering-history empty block, and a new "Plant not found" state for 404s on the detail page. Load-error banners kept inline.
- `feature/pwa-shell` — done. Standalone `manifest.webmanifest` (theme color #16a34a, 192/512 + maskable icons), SVG favicon + `favicon-32.png` fallback + `apple-touch-icon`, meta links in `index.html`, nginx serves the manifest as `application/manifest+json`. Icons generated by `scripts/generate-icons.mjs` (pure Node PNG writer). No service worker (offline stays out of v1 scope).
- `feature/plant-thumbnails` — done. `PlantPhoto` component: lazy `<img>` from `PhotoUrl` with a dashed sprout placeholder when absent or broken; thumbnails on `PlantCard` (skeleton mirrored), photo block on detail replacing the "Photo" text field.
- `feature/photo-upload` — done. Storage decision (architecture.md #7): API writes to the `plant-photos` volume at `{PHOTO_STORAGE_PATH}/plants/{id}/{guid}` (jpeg/png/webp/gif, 5 MB cap); nginx serves `/uploads/*` read-only from the same volume. `POST /api/plants/{id}/photo` multipart endpoint + old-file cleanup, `useUploadPlantPhoto` hook with toasts, "Upload photo" button on detail. Form keeps its external-URL field.
- `feature/plant-card-redesign` — done. `DueCount` (days-until-due as the primary number; red for overdue) + short status badge with icon **and** text (no color-only meaning) + "Watered N days ago" relative line (`lib/dates.ts`) on `PlantCard`; skeleton mirrored. Note: badge labels ("Overdue") now coexist with dashboard section headings — tests target headings by role.
- `feature/care-tips-markdown` — done. `@tailwindcss/typography` added; care notes render through `prose prose-sm prose-neutral dark:prose-invert` in `CareTipsCard` (was ad-hoc `[&_h1]` classes). Deviation: optional TOC skipped — seed tips average ~140 chars; revisit in `seed-expansion`.
- `feature/mobile-layout-pass` — done. Shared `lib/ui.ts` touch classes (`min-h-11` buttons, `h-11 text-base` fields below `sm`); sidebar trigger + theme toggle enlarged on phones; wrapping headers/action rows; `break-words` names. Epic 2 complete.
- `feature/optimistic-watering` — done. Water button on dashboard/list `PlantCard`s (via `onWater` prop); `useWaterPlant` patches cached list+detail in `onMutate`, rolls back on error, and the success toast carries **Undo** → new `DELETE /api/plants/{id}/water` endpoint (removes newest log, rewinds LastWateredAt, no-op without logs). Delete now uses a shadcn `alert-dialog` confirm instead of `window.confirm`.
- `feature/plant-form-ux` — done. `ProfileCombobox` (Popover+Command) replaces the profile Select with type-ahead search; live `role="status"` next-due preview (custom interval overrides profile default); `ApiError` now carries `ProblemDetails.errors`, `splitApiError` maps them to camelCase per-field messages with `aria-invalid` (banner kept only for non-field errors). Acquired-date-today default already existed.
- `feature/watering-history-viz` — done. `WateringHistoryChart` on the detail card: dependency-free monthly bars (last 6 months, `chart-1` token) over the existing logs endpoint, sr-only data table, hidden when the window is empty. Test timeout raised for cmdk-heavy suites.
- `feature/plant-search-filters` — done. Search (name/species/location) + due-status filter + sort (name/location/soonest due/recently watered) on the plants list via pure `filterPlants` (`lib/plantFilters.ts`); "N of M" count; distinct "No matching plants" empty state with clear-filters action. cmdk combobox test given an explicit 30s timeout (flaky under default 5s in jsdom).
- `feature/keyboard-shortcuts` — done. `KeyboardShortcuts` in the shell: `n` new plant, `w` water focused card (focusable `data-plant-card` + `data-water-button`), `/` focus `#plant-search`; ignored while typing or with modifiers.
- `feature/routing-polish` — done. `Breadcrumbs` on list/detail/form (detail ends with the plant name), `ScrollRestoration` keyed on router location, list filters moved to URL search params (`?q=&due=&sort=`) for shareable/back-restorable deep links; detail URLs already existed.
- **Epic 3 ships as one PR: `epic/watering-care-ux`** (optimistic-watering and plant-form-ux were merged individually before the one-epic-one-PR switch; viz + search + shortcuts + routing ride the epic branch). Next epics follow the new convention.
- **Epic 4 ships as one PR: `epic/dashboard-views`** — all five chunks done.
  - `feature/dashboard-stats` — `DashboardStatsStrip` (overdue / due today / total tiles, `role="group"` labels, red-on-overdue, green "All caught up" band when nothing needs water).
  - `feature/calendar-view` — `/calendar` route + nav entry; week (next 7 days) and month (navigable) grids from `projectOccurrences` (`lib/scheduleProjection.ts`, clamps overdue to today); a11y labels per day.
  - `feature/ical-feed` — `GET /api/calendar.ics` (`CalendarService`: recurring all-day VEVENTs with RRULE/UID, CRLF + folding + escaping) and nginx `= /calendar.ics` proxy so phones can subscribe on the LAN.
  - `feature/wall-mode` — `/wall` outside the shell: auto-refreshes the dashboard every 60s (`useDashboard(refetchMs)`), live clock, shared `dashboardSections` extraction.
  - `feature/collection-insights` — `GET /api/insights` (`InsightsService`: totals, species diversity, most-neglected, 30-day adherence vs expected waterings, on-time streaks, 12-month counts) + `/insights` page with tiles, monthly bars (sr-only table) and three list cards.
- **Epic 5 ships as one PR: `epic/rooms-environment`** — two of three chunks done:
  - `feature/room-entity` — `Room` (name unique, orientation) + EF migration that copies distinct `Plants.Location` values into rooms and rewires `Plant.RoomId` (free-text column dropped); rooms CRUD (`/api/rooms`, 409 on duplicate, SetNull on delete); room picker + inline "quickly add a room" in the plant form; `/rooms` admin page with per-room plant list; dashboard `?group=room` view. ntfy text now uses the room name.
  - `feature/room-environment` — optional room params (light exposure, humidity, temperature °C) with a second migration; `RoomLightMatch` computed server-side (profile `LightRequirement` vs room exposure, `Good/Slightly*/MuchToo*`), surfaced as `RoomLightBadge` on detail and in the rooms view ("wrong room" flag at ≥2 levels).
  - `feature/room-sensors` — **deferred: the plan itself flags this as needing a design decision first** (which smart-home source: MQTT/HA, read-only sync model, secrets). No code was invented for it; revisit as its own epic-6-style slice once the source is chosen.
- **Epic 6 ships as one PR: `epic/care-tasks`** — all six chunks done (this was the deliberate invasive-migration window; the API surface stayed compatible throughout):
  - `feature/care-task-model` — typed `CareTask` (`Watering`/`Fertilizing`/`Repotting`) + `CareTaskLog` replace `WateringLog` + the plant's interval/last-watered columns (`AddCareTasks` migration moves all data); `/water` and `/watering-logs` kept working unchanged; generic `GET/POST/DELETE /api/plants/{id}/care-tasks*` with "mark as done" per type; insights/adherence/streaks now computed from tasks.
  - `feature/fertilizing-schedule` — Fertilizing task type with add/remove/mark-done UI in `CareTasksCard` + server hints: winter rest (Dec–Feb) and flush-soil reminder at ~120 days (`CareTaskHints`).
  - `feature/seasonal-intervals` — `ReduceInWinter` (nullable per task, `DefaultReduceInWinter` per profile); winter doubles the effective interval in the single-source schedule service. v1 non-goal explicitly revisited and lifted.
  - `feature/repot-lifecycle` — `PotSizeCm`/`SoilMix`/`PropagatedFrom` on the plant (form + detail), yearly Repotting task; last/next repot from task data.
  - `feature/water-details` — optional `amountMilliliters` + `method` (Tap/Filtered/Rainwater) on watering logs, "log a watering" form on detail.
  - `feature/plant-notes` — `PlantNote` entity + `GET/POST /api/plants/{id}/notes` + notes section on detail.
- **Epic 7 ships as one PR: `epic/notifications`** — all four chunks done.
  - `feature/digest-notifications` — per-plant ntfy spam replaced by one daily digest (`N plants need water`, bulleted), priority 5 escalation when overdue; per-plant `NotifyEnabled` mute (form checkbox); `NotificationDigest` table (replaces per-plant `NotificationLog`, data migration drops it).
  - `feature/vacation-snooze` — `SnoozedUntil` per plant + `POST /api/plants/{id}/snooze`, `DELETE …/snooze`, `POST …/snooze-all`; digest collection skips active snoozes; detail snooze picker (3/7/14/30d, resume) + dashboard "Snooze all".
  - `feature/ntfy-quick-actions` — secret-guarded `GET/POST /api/plants/{id}/quick-water?key=` (constant-time compare, sliding-window rate limit 10/plant+IP/10min, logged), single-due-plant digest carries an ntfy action button built from `QUICK_ACTION_URL_BASE`.
  - `feature/extra-channels` — `INotificationChannel` abstraction (ntfy always; optional Telegram via Bot API when `TELEGRAM_BOT_TOKEN`+`TELEGRAM_CHAT_ID` set, markdown digest + water link). Deviation: email unimplemented — SMTP provider/config needs its own decision.
- **Epic 8 ships as one PR: `epic/species-knowledge`** — five of six chunks done.
  - `feature/profile-management-ui` — `/profiles` page over the existing CRUD endpoints; profile responses now carry light/humidity/careTips/toxicity/checklist + `plantCount`.
  - `feature/seed-expansion` — bundled seed 8→14 species with curated tips; `SeedLoader.LoadCustomProfilesAsync` scans `SEED_CUSTOM_PATH` (default `/data/seed-custom` on the plant-data volume) at startup — drop-in JSON, dedup by name, malformed files logged and skipped.
  - `feature/species-catalog-search` — **deferred: provider decision required** (external catalog data quality, rate limits, offline behaviour). Not built.
  - `feature/toxicity-flags` — `ToxicToPets`/`ToxicToChildren` on profiles (migration + seed annotations), warning icon on cards (accessible label), destructive badges on detail, `[toxic to pets]` tags in the watering digest.
  - `feature/diagnostics-checklist` — per-profile `DiagnosisChecklist` JSON (server-validated, `InvalidChecklist` → 400), seeded for 5 species, rendered as collapsible symptom→cause cards on detail, editable in the profiles form.
  - `feature/care-journal` — `JournalEntry` entity + multipart `POST /api/plants/{id}/journal` (date/note/photo via the storage volume under `plants/{id}/journal/`), list + delete endpoints, `JournalCard` on detail with entry list and before/after comparison.
- **Epic 9 ships as one PR: `epic/platform-data`** — all seven chunks done.
  - `feature/bulk-water` — `POST /api/plants/bulk-water {ids}` (per-plant loop in the service, one response summary); "Water all" button on every non-upcoming dashboard bucket in both grouping modes.
  - `feature/data-export-import` — `GET /api/export` snapshot (rooms, profiles, plants incl. care tasks + logs, notes, journal) and `POST /api/import` merging by natural keys (idempotent); export button + import file picker on the Status page; backup/volume docs in the root README.
  - `feature/openapi` — ASP.NET Core built-in OpenAPI (`AddOpenApi`/`MapOpenApi`, no Swashbuckle dependency), served at `/openapi/v1.json` and proxied through nginx.
  - `feature/app-status` — `GET /api/status`: last job run (new `JobRuns` audit rows written on every check), last digest, live ntfy probe (2 s timeout, never crashes), cron, time zones; `/status` page (nav item) with the `healthApi.status` client addition.
  - `feature/timezone-config` — `TZ` env for the api container (compose + `.env.example`; cron fires in that zone), status echoes server tz; naive-UTC parsing centralized in `lib/dates.ts` (`parseInstant` only appends `Z` when no tz designator).
  - `feature/onboarding` — "Getting started" checklist on the empty dashboard: add-plant CTA + ntfy subscribe link built from `/api/status` (topic + reachability from config).
  - `feature/postgres-profile` — optional `postgres` compose profile (`db` service + `pg-data` volume): Npgsql provider switch, migration history made provider-portable (explicit value-generation strategies, no hard-coded column types, legacy timestamp behavior), retry-until-ready migration; verified end-to-end against PostgreSQL 17 (full migration chain + CRUD/water/ics/export smoke).
- **Second-language support ships as `feature/second-language-support`** — Polish (`pl`) alongside English, done.
  - Backend: `PlantProfileTranslation` table (unique per profile+language, field-level EN fallback) + migration; `Seed/plant-profiles.pl.json` with translated species names/humidity/tips/diagnosis upserted at startup; static EN/PL message catalog behind scoped `IAppLocalizer` with `Accept-Language` resolution (`APP_LANGUAGE` env as the no-request-context default) localizing due messages, digest (ntfy + Telegram, incl. the button label), care-task hints, ICS summaries, quick-action responses and ProblemDetails; profile content (care tips, common names) served per request language; PUT profile writes route to the translation row in non-EN languages.
  - Frontend: `i18next`/`react-i18next` with bundled `en`/`pl` catalogs; `LanguageToggle` next to `ModeToggle`; preference in `localStorage` (`ui-language`), browser-language default, `<html lang>` synced; every UI string and mutation-hook toast translated with correct Polish plural forms; `Accept-Language` sent by the API client; dates/month names and list collation follow the active language; language change invalidates all queries.
- **Species seed expansion ships as `feature/plant-speacies`** — done. Bundled seed 14→44 species (ferns, hoyas, pilea, philodendrons, orchid, succulents, palms, etc.) with curated tips, watering intervals 3–21 days, light and toxicity data — added to both `Seed/plant-profiles.json` and `Seed/plant-profiles.pl.json` (Polish names, humidity notes and care tips). No schema change: the startup upsert delivers new species to existing databases automatically. Deviation: the `PlantProfileWriteApiTests` fixture profile renamed "String of Pearls" → "String of Diamonds" (the former is now seeded, 409 on create).
- **First-launch configuration ships as `feature/first-launch-configuration`** — done.
  - Frontend-only: `SetupGate` wraps the shell and launches `SetupWizard` when the plants and rooms lists are **both empty** (the first-launch signal — no data is persisted about setup state, and nothing in code auto-creates rooms/plants; only the species profile catalog is seeded). The wizard walks Welcome → Rooms (quick name-only CRUD) → Plants (full `PlantForm`, add several in a row) → Theme & language (writes through `useTheme`/`setLanguage`) → summary, with per-step skip. Decision latches per page load; errors fall through to the normal app. Extends `feature/onboarding` (the empty-dashboard checklist stays as the post-setup reminder).
- **Photo picker in plant forms ships as `feature/photo-upload-picker`** — done. Extends Epic 2's `feature/photo-upload` to the add/edit plant form (and the setup wizard's plant step): shared `PhotoPicker` component — file button (`accept` jpeg/png/webp/gif, camera on phones), thumbnail preview, "Remove", inline 5 MB / type guard before any request; the picked file uploads through the existing `POST /api/plants/{id}/photo` right after create/update via new `useUploadPlantPhotoToId` (per-plant endpoint needs an id first). The external-URL field stays below as a fallback; a picked file replaces it server-side. `PlantForm.onSubmit` now receives `(input, photoFile)`.
- **Pickable soil type affecting the watering schedule ships as `feature/soil-type-schedule`** — done. A `SoilType` enum (`AllPurpose`/`CactusMix`/`ChunkyBark`/`PeatCoco`/`SemiHydro`/`SelfWatering`, `AddSoilType` migration, stored as text) on the plant models substrate water-permeability. `SoilTypes` maps each type to a factor and `WateringScheduleService` scales the effective watering interval (task/profile base → soil factor → winter doubling), so a fast-draining mix dries sooner (water earlier) and a retentive/reservoir mix later — still a single source of truth for due-date math, reused by the dashboard, iCal and digest. New `GET /api/reference-data/soil-types` serves the options + factors so the frontend stays server-driven; the plant form gets a soil-type `Select` and the live next-due preview applies the factor (`src/lib/soilTypes.ts`, rounding matching the backend) with a hint explaining the change; detail shows the label. Enum travels as a stable PascalCase key (localized labels are a presentation concern). Free-text `SoilMix` stays untouched (recipe note vs. permeability category); no new env vars.
- **Snooze watering when the soil is still wet ships as `feature/soil-wet-snooze`** — done. A separate `Plant.SoilWetUntil` deferral (`AddSoilWetSnooze` migration) — deliberately distinct from Epic 7's notification-only vacation `SnoozedUntil`. `WateringScheduleService.GetDueInfo` clamps the **watering** due date out to the recheck day while it's in the future (never earlier; other care-task types untouched; watering clears it), so the plant leaves the overdue/due buckets and the daily digest drops it without extra skip logic, then resurfaces on its own once the deferral lapses. `POST`/`DELETE /api/plants/{id}/soil-wet` (days 1–30, `[Range]`-validated) extend from an active deferral; `PlantService.WaterAsync` and the watering care-task "mark done" both clear it; export/import carry the field. Frontend: detail-page "Soil is still wet" control (`useSetSoilWet`/`useClearSoilWet`, 2/3/5/7-day recheck picker + active "deferred until …"/"Check now"), a read-only "Soil wet — recheck {date}" note on `PlantCard`, all EN/PL localized. No new env vars.


## Suggested sequencing

1. Epics 1 and 3 first — they change how the app feels with zero schema risk.
2. Epic 5 (rooms) before Epic 9's bulk-water and before room-dependent views.
3. Epic 6's `care-task-model` is the one invasive migration — schedule a deliberate window, keep watering behavior intact.
4. Epics 7 and 8 can interleave with 2 and 4 based on interest.

## Explicitly deferred / not planned

- Multi-user auth, mobile app, cloud sync (v1 non-goals, still rejected).
- Self-hosted vision-model plant diagnosis (experimental branch off `feature/diagnostics-checklist`, only if ever wanted).
