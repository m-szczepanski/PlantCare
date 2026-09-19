# Improvement Plan (post-v1)

Deliverable chunks derived from `ideas.md`. Each chunk is one scoped branch/PR (repo convention: `feature/<name>`), sized to be reviewable on its own and shippable without breaking v1. This is a proposal — reorder freely; only the listed dependencies are hard.

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

## Suggested sequencing

1. Epics 1 and 3 first — they change how the app feels with zero schema risk.
2. Epic 5 (rooms) before Epic 9's bulk-water and before room-dependent views.
3. Epic 6's `care-task-model` is the one invasive migration — schedule a deliberate window, keep watering behavior intact.
4. Epics 7 and 8 can interleave with 2 and 4 based on interest.

## Explicitly deferred / not planned

- Multi-user auth, mobile app, cloud sync (v1 non-goals, still rejected).
- Self-hosted vision-model plant diagnosis (experimental branch off `feature/diagnostics-checklist`, only if ever wanted).
