---
name: plantcare-step
description: Implements PlantCare features strictly following docs/implementation-plan.md. Use when the user asks to implement, build, or continue a step, feature slice, or milestone of the PlantCare app.
---

# PlantCare Step Workflow

Use this workflow whenever implementing anything in the PlantCare repo. The source of truth for what to build is `docs/implementation-plan.md`.

## Before coding

1. Read `AGENTS.md`, then the docs relevant to the step (`docs/overview.md`, `docs/architecture.md`, `backend/docs/README.md` or `frontend/docs/README.md`).
2. In `docs/implementation-plan.md`, find the first step whose acceptance criteria are not all met. Never skip ahead.
3. Announce which step you are starting and list its acceptance criteria as a todo list.

## While implementing

- Follow the step's **How** section; match the conventions in `AGENTS.md` (thin controllers, DTOs over entities, typed API client, no raw fetch in components, etc.).
- Backend: business logic in `Services/`, new behavior covered by tests in `PlantCare.Api.Tests/`. Schema changes always ship with an EF Core migration in the same commit.
- Frontend: hooks + typed API client, shadcn composition, theming only via `tailwind.config.ts`.
- Do not add dependencies, infra (Hangfire/Quartz, Postgres), or features beyond the step's **What** — that is scope creep against documented non-goals.
- New env vars must be added to `.env.example` with a comment.

## Verification (before finishing)

1. `dotnet build backend/PlantCare.Api` and `dotnet test backend/PlantCare.Api.Tests` pass.
2. `npm run build` and `npm run test` pass in `frontend/`.
3. `docker compose config` is valid (full `docker compose up --build` when the step touches containers).
4. Walk through the step's acceptance criteria checklist one by one. Report each as passing or explain what blocks it.
5. If the implementation deviated from a documented decision, update the relevant doc and note the deviation.

## Committing

Only commit when the user asks. Keep commits scoped to the step or a coherent slice, message style: `add watering scheduler`, `add PlantProfile CRUD`.
