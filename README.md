# Hackathon Ticketing System

Kanban-style ticket tracker built as a three-tier SPA: React frontend, ASP.NET Core
API, and PostgreSQL. See `IMPLEMENTATION_PLAN.md` for architecture and phases, and
`Hackathon_Ticketing_System_Requirements_v4.docx` for the authoritative requirements.

## Prerequisites

- **Docker Desktop** (the only required runtime). No host-installed .NET or Node is
  needed to run the stack.
- For local iteration outside Docker (optional): .NET 10 SDK and Node.js 18+.

## Run the whole stack

```bash
cp .env.example .env
docker compose up --build
```

| Service | URL |
| --- | --- |
| Frontend (SPA) | http://localhost:8080 |
| Backend API | http://localhost:5083 |
| API health check | http://localhost:5083/health |
| OpenAPI document | http://localhost:5083/openapi/v1.json |
| MailPit (captured email) | http://localhost:8025 |

The database schema is created automatically via EF Core migrations on API startup.
A fresh database contains schema + migration metadata only — no seed data.

## Configuration

All configuration is via environment variables (see `.env.example`):

- `POSTGRES_USER` / `POSTGRES_PASSWORD` / `POSTGRES_DB` — database credentials.
- `ConnectionStrings__Default` — API connection string (set for you in compose).
- `SMTP__Host` / `SMTP__Port` — email relay. Dev/demo uses the bundled **MailPit**
  container; for production set `SMTP__Host=relay1.dataart.com`, `SMTP__Port=25`.

## Local development (optional, outside Docker)

Backend (from the repository root):

```bash
dotnet tool restore                                          # restores dotnet-ef
dotnet run --project src/HackathonTaskTicketingSystem.Api    # http://localhost:5083
```

Frontend:

```bash
cd frontend
npm install
npm run dev
```

Both require a reachable PostgreSQL instance; the containerized stack above is the
source of truth for "does it work".
