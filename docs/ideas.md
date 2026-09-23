# Ideas Backlog

Improvement ideas collected after the v1.0 release. This is a parking lot, not a plan — items are rough and unordered; `improvement-plan.md` groups them into deliverable chunks with proposed branches. Guardrails from `AGENTS.md` (no multi-user auth, no extra infra, single-user focus) still apply.

## UI Improvements

- Theme toggle (light/dark/system) — dark tokens already exist in `index.css` but nothing switches them; needs a small theme provider + persisted preference (localStorage, respecting `prefers-color-scheme` by default).
- App shell/navigation: a persistent header or sidebar with Dashboard / Plants links and the theme toggle; current pages have no global nav.
- Plant photos: thumbnails on plant cards and detail view (from the existing `PhotoUrl`); a proper placeholder illustration when absent.
- Photo upload (deferred from v1): store under the `/data` volume, serve via API/nginx — needs a storage decision first.
- Species/profile management UI: the API supports profile create/update, but editing care tips and defaults currently requires HTTP calls by hand.
- Watering history visualization on plant detail: simple timeline or "last N waterings" dots / bar per month.
- Dashboard stat strip: counts (X due, Y overdue, Z plants) with a friendly empty/green state.
- Plant card polish: due-soon color gradient on the badge, days-until-due as the primary number, relative "watered 3 days ago" text.
- Mobile layout pass: the dashboard/list at phone width (the app is mostly used on a phone via LAN), touch-friendly buttons.
- Consistent empty-state illustrations instead of plain text in empty sections.
- Markdown rendering tweaks for care tips: styled headings/lists/table of contents for long tips.
- Loading skeletons matching card layout instead of spinners/text.
- Favicon + PWA manifest + icons (installable on phone home screen without a native app — stays within non-goals).
- Wall/tablet dashboard mode: read-only auto-refreshing full-screen route (no chrome, big cards) for a home tablet — turns the app into an ambient plant-care display.
- Upcoming-care calendar view: week/month grid of scheduled waterings/tasks alongside the bucket-based dashboard.

## Functionality Improvements

- First-class rooms with orientation and environment: promote `Plant.Location` (free text) to a `Room` entity — name, window orientation (N/E/S/W, none), and optional measured parameters (average light exposure, humidity, temperature). One room per space (no sub-sites); plants reference their room. The app matches a profile's `LightRequirement` (and humidity notes) against the room: recommends sun-lovers for south/west rooms and low-light-tolerant ones for north rooms, flags plants sitting in the wrong room ("this Monstera wants more light — try the south one"), and offers a per-room view ("plants that suit this room", free capacity). Optional later step: feed the room's humidity/temperature from existing smart-home sensors instead of manual values.
- iCal feed for the care schedule (`GET /api/calendar.ics`): subscribe from any calendar app so waterings show up next to the rest of the week.
- Care journal / growth log: dated photo + note entries per plant ("growth time-lapse"), distinct from watering notes; feeds a before/after comparison view.
- Plant diagnostics helper: structured symptom checklist per species ("yellow leaves → likely overwatering") stored in the profile; optional experimental step — self-hosted vision model on a journal photo (flag-gated, out of v1 scope creep territory).
- Repot & substrate lifecycle: pot size, soil mix, last-repot date, next-repot reminder (soil refresh intervals), drainage notes per plant.
- Fertilizing as a first-class schedule with flush/season reminders, not just free-text care tips.
- Water details per plant: amount, method (tap/filtered/rainwater) and notes — matters for species sensitive to chlorine/fluoride.
- Toxicity awareness: `ToxicToPets`/`ToxicToChildren` flags on the profile with warning badges on cards and in the notification text.
- Collection insights page: total plants, species diversity, most-neglected plants, watering adherence/streak stats, seasonal trends — read-only aggregation over existing logs.
- Bulk actions: "water all" in a dashboard bucket or per location (currently one-by-one only).
- Care task types beyond watering (misting, fertilizing, repotting, rotating): generalize `WateringLog`/schedule into a per-plant `CareTask` with type + interval; "mark as done" per task type.
- Seasonal watering adjustments (resolved non-goal in v1): interval multipliers per season or a simple "reduce in winter" flag per profile/plant.
- Due notification improvements: single digest message instead of one ntfy post per plant; overdue escalation (e.g., re-notify at higher ntfy priority after N days); configurable per-plant notification on/off.
- Additional notification channels behind the same publisher abstraction (e.g., email via SMTP, Telegram) — optional, env-gated.
- Quick actions from the notification itself (ntfy actions / HTTP trigger endpoint) to water a plant without opening the app — needs a lightweight shared-secret URL since there is no auth.
- Search and filter/sort on the plants list (by name, location, due status, species).
- Group plants by location (room) on the dashboard or as an alternate view.
- Plant health status / free-text notes on the plant itself (issues, observations), separate from the profile's care tips.
- Periodic health checkups: a four-level status per plant (sick / bad / good / excellent) recorded via a monthly checkup prompt; the latest answer adjusts the watering/fertilizing schedule until the next checkup (less water + paused feeding while unwell, slightly more while thriving).
- Unwatered/absence tracking: mark a plant "on vacation" / snooze notifications for N days.
- Data export/import (JSON) for backup and moving to a new install; SQLite file backup guidance in README.
- Wider plant catalog with type-ahead species search: instead of picking from the small seeded list, the plant form gets a search-as-you-type combobox backed by an external species database (e.g., Plants-US / Trefle / Perenote API), so any plant the user owns can be found and matched to care defaults. Needs a provider choice (data quality, rate limits, offline-friendliness) and a strategy for caching/importing matches into local `PlantProfile`s.
- Seed data expansion: more common species, curated care-tip content, and a way for users to add species without the (still missing) profile UI (e.g., drop-in JSON folder watched at startup).
- Propagation/repot dates and age ("owned since") shown on detail; acquired-date anniversary note.
- Optional Postgres compose profile finally wired and documented (provider switch was designed in, never exercised).
- Basic API versioning/OpenAPI (Swashbuckle) JSON surfaced for power users before any real versioning need.

## UX Improvements

- Optimistic "mark as watered": instant feedback + undo toast (currently requires a round-trip; a mis-click is immediately logged).
- Confirm dialog on delete; undo (soft delete or toast-based restore) instead.
- Inline watering directly on the dashboard plant cards (was explicitly deferred; now trivial with shared `PlantCard`).
- Better form UX: profile picker with search/combobox, live "next watering due" preview as the interval changes, sensible defaults (acquired date = today), autosave drafts.
- Validation feedback: inline field errors from the API's 400 responses instead of a generic banner.
- Keyboard shortcuts: `n` new plant, `w` water focused card; `/` to focus search (once search exists).
- Toast/notification system for all mutations (success + failure), replacing any ad-hoc messaging.
- Onboarding flow for a fresh install: friendly first-run dashboard explaining ntfy subscription with a link/button to subscribe to the configured topic.
- In-app status page: ntfy reachability, last notification job run, scheduler health (the reserved `healthApi` could feed this).
- Timezone awareness: notification cron is server-time; document or allow a `TZ` and show dates in local time consistently (v1 audit already fixed naive-UTC parsing in the UI).
- Accessible due status: badge color is currently the main cue — add text/icons with aria labels for color-blind users.
- Deep-linkable, shareable URLs for plant detail; breadcrumbs back to list/dashboard.
- Remember last visited tab/filter; scroll restoration on back navigation.
