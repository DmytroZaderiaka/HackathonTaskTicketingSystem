# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

A hackathon project: a **Kanban-style ticket tracker** built as a three-tier SPA. The authoritative spec is `Hackathon_Ticketing_System_Requirements_v4.docx` in the repo root — **read it before making scope or behavior decisions**; the rules below are a working summary, not a replacement.

Current state: the backend is a bare ASP.NET Core scaffold (the `WeatherForecast` template has been removed). Frontend, database, persistence, and the ticketing domain are **not built yet**. Treat most of this file as the *target* architecture.

## Working agreement

- **Communicate with the developer in Russian** — explanations, architecture discussion, and chat responses are in Russian.
- **All code artifacts are in English**: identifiers (variables, methods, classes), comments, and XML documentation.
- **Explain the implementation plan in Russian before modifying code.**
- Keep commits small and focused. **Avoid unnecessary refactoring** — change only what the task needs.
- **Inspect the existing solution/project structure before adding new files** — place them consistently and avoid duplication.
- After significant changes, **verify that `docker compose up --build` still works**, or explain why it could not be verified (e.g. Docker unavailable in the environment).

## Stack & topology (decided)

- **Frontend**: React + Vite + TypeScript (SPA).
- **Backend**: ASP.NET Core Web API on .NET 10 (`HackathonTaskTicketingSystem/`).
- **Database**: PostgreSQL. Schema created via **migrations** (EF Core is the natural fit); a fresh DB must contain schema + migration metadata only — **no seed/sample data on the default startup path**.
- **Deployment**: three separate containers — frontend (nginx serving the built SPA), backend API, PostgreSQL — orchestrated by Docker Compose.
- **Hard requirement**: from a clean checkout, `docker compose up --build` at the repo root must bring up the entire stack with **no host-installed runtimes** beyond Docker. QA runs this on a clean Windows/macOS/Linux laptop.

## Commands

Backend (run from repo root, the directory with `HackathonTaskTicketingSystem.slnx`):

```powershell
dotnet build
dotnet run --project HackathonTaskTicketingSystem            # http://localhost:5083
dotnet watch --project HackathonTaskTicketingSystem run      # hot reload
dotnet test                                                  # once a test project exists
dotnet test --filter "FullyQualifiedName~SomeTest"           # single test
```

There is no test project yet; the spec requires **at least one backend business-flow test and at least one frontend/API-flow test**. When adding a backend test project, create it as a sibling and register it in `HackathonTaskTicketingSystem.slnx`.

The OpenAPI document is at `/openapi/v1.json` (Development only; see `Program.cs`).

> Note: local `dotnet`/`npm` runs are fine for fast iteration, but the source of truth for "does it work" is `docker compose up --build` from a clean checkout.

## Domain model

Entities: **User, Team, Epic, Ticket, Comment**. Relationships and the rules the backend must enforce (client-side validation alone is insufficient):

- **Team** → has many Epics and Tickets. Name non-empty after trim, unique case-insensitively. **Cannot be deleted while it holds tickets or epics → HTTP 409**. No cascading delete. No ownership/membership — all verified users manage all teams.
- **Epic** → belongs to exactly one Team, set at creation and **immutable** (no moving between teams). Title non-empty after trim. **Cannot be deleted while tickets reference it → HTTP 409**.
- **Ticket** → belongs to a Team; may optionally reference an Epic, **but only an epic of the same team** (backend-enforced). Fields: `type` ∈ {`bug`,`feature`,`fix`}; `state` ∈ {`new`,`ready_for_implementation`,`in_progress`,`ready_for_acceptance`,`done`} (canonical API values; UI shows human labels). Title + body non-empty. `created_at`/`modified_at` server-set in UTC; `created_by` from the authenticated user. Deleting a ticket deletes its comments.
- **Comment** → belongs to a Ticket; has author, non-empty body, created timestamp. Immutable in mandatory scope. Displayed oldest-first.

### Behavior rules that are easy to get wrong

- **`modified_at` tracks real field/state changes only.** Saving unchanged values must NOT advance it. **Adding a comment must NOT advance it** (so it doesn't reorder the board).
- When a ticket's **team changes**, the UI must clear/replace the selected epic, and the backend must reject an epic from a different team.
- **Board column ordering** is by `modified_at` descending (most recently modified first). No persisted manual order.
- Drag-and-drop state change must persist immediately via the API; **on failure the card returns to its previous column and an error is shown**. Any state→any state is allowed (no sequential-transition enforcement).
- Filters (by type, by epic) + case-insensitive substring search on title combine with **AND**. Board must stay usable with 100+ tickets.

## Authentication

- Local email + password only (no SSO/OAuth). Email is **trimmed, compared case-insensitively, unique**.
- Passwords ≥ 8 chars, **never stored plaintext, hashed with Argon2id** (or equivalent established algorithm).
- **Email verification required** before using the app. Sent via configurable SMTP — the implementation must support `relay1.dataart.com`. Verification tokens **expire in 24h, single-use**; issuing a new one invalidates earlier unused tokens. Unverified users can request a resend.
- All app screens/endpoints require auth **except** sign-up, login, email verification, and resend. Static assets and optional health endpoints may be public.
- Cookie sessions or bearer tokens both fine, but **session ids/tokens must never appear in URLs** (the single-use email-verification token in the verification URL is the only exception).

## API & persistence conventions

- All create/update/delete goes through the API and persists in PostgreSQL; **browser local storage is never the system of record**.
- Return meaningful status codes: validation, auth failure, not-found, and **409 Conflict** for delete-blocked-by-reference cases.
- IDs may be UUID or DB-generated numeric. API timestamps are **ISO-8601 UTC**.
- Last-write-wins; no concurrent-edit conflict detection required.
- Keep SMTP secrets and credentials out of source control. Declare required environment variables in a committed **`.env.example`** (keys + placeholder values only); **never commit real secrets**.

## Out of scope (don't build)

Scrum/sprints/backlogs/story points, SSO/OAuth, roles/admin/team-membership/private teams, file attachments, notifications/mentions/watchers, audit history, real-time multi-user updates, custom workflows/types, subtasks, time tracking, reporting dashboards, production-grade deployment/HA/mail.

## Code conventions

The backend `.csproj` enables **`Nullable`** (reference types non-nullable by default; annotate nullable with `?`) and **`ImplicitUsings`** (common namespaces auto-imported — don't add redundant `using` directives). `Program.cs` is the single composition root: register services, middleware, and dependencies there.

### Backend architecture principles

- Follow **ASP.NET Core best practices** and **official Microsoft naming** conventions.
- Prefer pragmatic clean architecture, avoid over-engineering for hackathon scope and apply **SOLID**.
- **`async`/`await` everywhere** for I/O; flow a **`CancellationToken`** through controllers, services, and EF Core calls where appropriate.
- Return API errors as **`ProblemDetails`** (RFC 7807) — including the validation, auth, not-found, and 409 Conflict cases described above.
