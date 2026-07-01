# Implementation Plan — Hackathon Ticketing System

> Рабочий план и архитектурные решения. Источник истины по требованиям —
> `Hackathon_Ticketing_System_Requirements_v4.docx`. Рабочие соглашения — `CLAUDE.md`.
> Горизонт: ~2 дня. Приоритеты: рабочее демо, выполнение Definition of Done, минимум трения.

## 1. Общая архитектура

Три логических яруса, разнесённые по контейнерам, + MailPit для писем в dev/demo:

```
┌──────────────┐     ┌────────────────────┐     ┌──────────────┐
│  Frontend    │     │   Backend API      │     │ PostgreSQL   │
│ React+Vite   │─────│  ASP.NET Core /API │─────│  (container) │
│ nginx :80    │ /api│  .NET 10  :8080    │ SQL │   :5432      │
└──────┬───────┘     └─────────┬──────────┘     └──────────────┘
       │                       │ SMTP
       │              ┌────────▼─────────┐
       └── proxy ─────│  MailPit         │  UI :8025 / SMTP :1025
                      │ (dev/demo only)  │
                      └──────────────────┘
      docker compose up --build  (единая сеть, из корня репозитория)
```

- **Frontend** — SPA, собирается в статику, раздаётся nginx. nginx проксирует `/api/*` на
  backend → один origin, нет CORS, топология скрыта.
- **Backend** — REST API на .NET 10. Аутентификация — **cookie-сессии** (HttpOnly, Secure),
  токены/идентификаторы сессии не в URL.
- **PostgreSQL** — отдельный контейнер с volume. Схема применяется миграциями на старте.
- **MailPit** — SMTP-приёмник с веб-UI для dev/demo; production использует `relay1.dataart.com`
  через ту же переменную окружения (см. §2, «Email»).

### Ключевые архитектурные решения

| Решение | Выбор | Почему |
|---|---|---|
| Структура backend | **Один проект, вертикальные слайсы** | За 2 дня выгода от границ-сборок нулевая, трение реальное. Clean-принципы держим папками/неймспейсами. |
| Доступ к данным | **`DbContext` напрямую в сервисах** | EF Core уже = Unit of Work + Repository. Свой репозиторий = дырявая абстракция и boilerplate. Сложные запросы → query-extensions. |
| Аутентификация | **Cookie-сессии** | Фронт и API за одним origin → HttpOnly-cookie проще и безопаснее JWT в JS. |
| Email (dev/demo) | **MailPit** | Убирает главный риск демо: `relay1` доступен только из внутренней сети. Один код, разные env. |

## 2. Структура репозитория

```
HackathonTaskTicketingSystem.slnx
src/
  HackathonTaskTicketingSystem.Api/        # единственный backend-проект (см. §3)
tests/
  HackathonTaskTicketingSystem.Tests/      # xUnit: ≥1 backend business-flow + ≥1 API-flow
frontend/                                  # React + Vite + TS (см. §4)
docker-compose.yml                         # api + db + frontend + mailpit
.env.example                               # ключи + плейсхолдеры, без реальных секретов
README.md                                  # prerequisites, config, запуск
CLAUDE.md
IMPLEMENTATION_PLAN.md
Hackathon_Ticketing_System_Requirements_v4.docx
```

**Email через env** (общий код `IEmailSender` → `SmtpEmailSender`, меняется только конфиг):
- dev/demo (`docker-compose.yml`): `SMTP__Host=mailpit`, `SMTP__Port=1025`, UI на `:8025`.
- production: `SMTP__Host=relay1.dataart.com` (через `.env` / compose-override).

## 3. Структура backend (вертикальные слайсы)

```
src/HackathonTaskTicketingSystem.Api/
  Domain/
    Entities/          # User, Team, Epic, Ticket, Comment, EmailVerificationToken
    Enums/             # TicketType (bug|feature|fix), TicketState (5 значений)
    Common/            # BaseEntity (Id, CreatedAt, ModifiedAt)
  Features/            # слайс = папка: Endpoints/Controller + DTO + Service + Validation
    Auth/              # signup, login, logout, verify-email, resend
    Teams/             # CRUD + правило 409
    Epics/             # CRUD + привязка к team + 409
    Tickets/           # CRUD, enum-валидация, epic-same-team, modified_at-семантика
    Comments/          # add, list (хронология)
    Board/             # запрос доски: колонки, сортировка modified_at desc, фильтры/поиск
  Infrastructure/
    Persistence/       # AppDbContext, EntityConfigurations, Migrations
    Auth/              # Argon2idPasswordHasher, verification-token logic
    Email/             # SmtpEmailSender
    QueryExtensions/   # IQueryable-расширения (напр. Tickets.ForBoard(teamId, filters))
  Common/
    ErrorHandling/     # ProblemDetails-мидлварь / exception handler (RFC 7807)
    Abstractions/      # IEmailSender, IPasswordHasher, IClock, ICurrentUser
  Program.cs           # composition root: DI, middleware, auth, EF, ProblemDetails
  appsettings*.json
```

Направление зависимостей `Domain ← Features ← Infrastructure` держим дисциплиной неймспейсов.
Каждый слайс инжектит `AppDbContext`, пишет нужный запрос, вызывает `SaveChangesAsync`.
`async/await` и `CancellationToken` — сквозь контроллеры → сервисы → EF.

## 4. Структура frontend

```
frontend/src/
  api/          # HTTP-клиент (fetch/axios), типизированные вызовы, error→UI, credentials: include
  features/     # auth/ teams/ epics/ tickets/ board/ — компоненты + hooks по фиче
  components/   # переиспользуемый UI: Loading, Empty, ErrorState, ConfirmDialog
  routes/       # маршрутизация + auth-guard (redirect на login)
  types/        # TS-типы, совпадающие с API DTO
  lib/          # утилиты: state→label ("ready_for_implementation"→"Ready for Implementation"), dnd-хелперы
frontend/{index.html, vite.config.ts, Dockerfile, nginx.conf, package.json}
```

Библиотеки: **React Router**, **TanStack Query** (серверный state, отмена, инвалидация),
**dnd-kit** (drag-and-drop с немедленной персистенцией и откатом при ошибке).

## 5. Схема БД

| Таблица | Ключевые поля | Ограничения / индексы |
|---|---|---|
| `users` | id, email, password_hash, is_verified, created_at | `UNIQUE(lower(email))` |
| `email_verification_tokens` | id, user_id→users, token_hash, expires_at, used_at | single-use, TTL 24h; новый инвалидирует прежние неиспользованные |
| `teams` | id, name, created_at, modified_at | `UNIQUE(lower(name))`, name не пустой после trim |
| `epics` | id, team_id→teams, title, description?, created_at, modified_at | title не пустой; team_id immutable |
| `tickets` | id, team_id→teams, epic_id?→epics, type, state, title, body, created_by→users, created_at, modified_at | epic.team_id == ticket.team_id (backend); индекс `(team_id, modified_at DESC)` |
| `comments` | id, ticket_id→tickets, author_id→users, body, created_at | `ON DELETE CASCADE` с ticket |

- Удаление team/epic со ссылками → **409 Conflict через приложение** (не голая FK-ошибка).
- Токены хранятся как хэш. Timestamps — UTC, в API отдаются ISO-8601.
- Fresh DB: только схема + метаданные миграций, **без seed-данных** на дефолтном старте.

## 6. Фазы реализации

| # | Фаза | Содержание | Итог |
|---|---|---|---|
| 0 | **Каркас** | solution, слайс-структура, docker-compose (api+db+front-stub+mailpit), `.env.example`, ProblemDetails, EF Core + 1-я миграция, health-endpoint, README-скелет | `docker compose up --build` поднимает пустой стек |
| 1a | **Auth (backend)** | signup, Argon2id, email-верификация (MailPit), login/logout, cookie-сессия, resend, `/me` | API auth-флоу работает (проверка через OpenAPI/MailPit) |
| 1b | **Auth (frontend) + тесты** | экраны signup/login/verify/resend + auth-guard; тест-проект + backend business-flow тест (signup→verify→login) | регистрация → письмо → верификация → вход через UI |
| 2 | **Teams** | CRUD + 409, экран управления | команды CRUD с валидацией |
| 3 | **Epics** | CRUD + привязка к team + 409, экран | эпики CRUD, персистятся |
| 4 | **Tickets** | CRUD, enum-валидация, epic-same-team, modified_at-семантика, детальный экран | тикеты CRUD |
| 5 | **Comments** | add + хронология, не двигает modified_at | комментарии с автором/временем |
| 6 | **Kanban** | 5 колонок, dnd-kit, immediate persist + откат, фильтры(type,epic)+поиск(title, CI, AND), 100+ тикетов | доска работает, drag сохраняется после refresh |
| 7 | **Polish + tests** | loading/empty/error, ≥1 backend- и ≥1 API-тест, финальная проверка Docker, README | все чекбоксы DoD |

Порядок = порядок зависимостей: auth → teams → epics/tickets → board.

## 7. План git-веток

`main` — только рабочее состояние. Короткоживущие feature-ветки, маленькие коммиты,
merge в `main` по завершении фазы. Ветки создаю только с явного разрешения.

```
main
 ├─ chore/scaffold-solution     (фаза 0)
 ├─ feat/auth                   (фаза 1)
 ├─ feat/teams                  (фаза 2)
 ├─ feat/epics                  (фаза 3)
 ├─ feat/tickets                (фаза 4)
 ├─ feat/comments               (фаза 5)
 ├─ feat/kanban-board           (фаза 6)
 └─ chore/polish-and-tests      (фаза 7)
```

## 8. Риски и легко упускаемые требования

- **SMTP relay1 недоступен на демо** → решено MailPit'ом; relay1 остаётся как prod-конфиг. *(главный риск, митигирован)*
- **`modified_at`** — НЕ двигать при сохранении без изменений и при добавлении комментария (влияет на сортировку доски).
- **Порядок карточек** — `modified_at DESC`, ручной порядок НЕ сохраняем.
- **Epic ↔ team** — при смене team тикета обнулять epic на UI; backend отклоняет чужой epic.
- **Docker с чистого чекаута** — фронт собирается в multi-stage Dockerfile, без host-runtime.
- **409 через приложение**, не через голую ошибку БД.
- **Case-insensitive** — уникальность email и team-name (`lower()`), поиск по title.
- **Токены/сессии не в URL** (исключение — single-use email-verification токен).
- **Fresh DB без seed** — только схема + метаданные миграций.
- **ProblemDetails** — заложить с фазы 0, чтобы не переделывать.
- **100+ тикетов** — индекс `(team_id, modified_at DESC)`; виртуализация опциональна.

## 9. С чего начинаем и почему

**Фаза 0 — каркас + Docker + health + первая миграция.**
`docker compose up --build` с чистого чекаута — hard-requirement и условие того, что QA вообще
запустит проект. Если оставить это на конец, риск не запуститься блокирует всю оценку.
Подняв скелет из четырёх контейнеров и один health-endpoint первым, получаем непрерывно
проверяемую основу — каждая следующая фаза добавляется в уже рабочий пайплайн.
Сразу закладываем ProblemDetails и слайс-структуру, чтобы не рефакторить позже.

## 10. Прогресс

### Фаза 0 — Каркас ✅ (ветка `chore/scaffold-solution`)

Сделано:
- Проект перенесён в `src/HackathonTaskTicketingSystem.Api/` (assembly `.Api`, root-namespace `HackathonTaskTicketingSystem`), `.slnx` обновлён.
- Local tool manifest `.config/dotnet-tools.json` + `dotnet-ef` 10.0.9.
- Пакеты: `Npgsql.EntityFrameworkCore.PostgreSQL` 10.0.2, `Microsoft.EntityFrameworkCore.Design` 10.0.9.
- `AppDbContext` (`Infrastructure/Persistence/`) + регистрация Npgsql; строка подключения из конфигурации/env.
- Автоприменение миграций на старте (`Database.MigrateAsync`); пустая миграция `InitialCreate` (пайплайн проверен).
- `ProblemDetails` (RFC 7807) + `GlobalExceptionHandler` (`Common/ErrorHandling/`).
- Публичный `GET /health`.
- Backend multi-stage `Dockerfile`; frontend-стаб (Vite+React+TS) + `Dockerfile` + `nginx.conf` (проксирует `/api/*` → `api:8080`, SPA-fallback).
- `docker-compose.yml`: `db` (postgres:17) + `mailpit` + `api` + `frontend`; `.env.example`; `README.md`.

Отклонения/заметки:
- Из глобальной конфигурации удалён недоступный приватный NuGet-источник (ломал restore); используются публичный `nuget.org` + MS offline.
- Прямой пин `Microsoft.OpenApi` 2.9.0 — закрывает `NU1903` в транзитивной 2.0.0 из шаблона (3.x несовместима с source-generator).
- Снят `UseHttpsRedirection` — TLS терминируется на nginx; API внутри compose работает по HTTP.
- Порты: frontend `8080`, API `5083`, MailPit UI `8025`.
- `dotnet build` (solution) и `npm run build` (frontend) проходят чисто (0 warnings). `docker compose up --build` проверен разработчиком на машине с Docker Desktop — стек поднимается, фронтенд видит API как reachable. (В агентском окружении Docker недоступен, поэтому проверка — на стороне разработчика.)

### Фаза 1a — Auth (backend) ✅ (ветка `feat/auth`)

Сделано:
- Сущности `User`, `EmailVerificationToken` + конфигурации; уникальный индекс по нормализованному `Email` (trim+lower → case-insensitive), уникальный индекс по `TokenHash`, FK с cascade. Миграция `AddUsersAndVerificationTokens`.
- Абстракции `IClock`/`IPasswordHasher`/`IEmailSender`/`ICurrentUser` + реализации: `SystemClock`, `Argon2idPasswordHasher` (Konscious), `SmtpEmailSender` (MailKit), `CurrentUser` (из cookie-принципала).
- `AuthService` + `AuthController`: `POST /auth/signup`, `GET /auth/verify-email`, `POST /auth/resend`, `POST /auth/login`, `POST /auth/logout`, `GET /auth/me`.
- Cookie-аутентификация (HttpOnly, SameSite=Lax, 401/403 вместо HTML-редиректов) + глобальная fallback-политика «require authenticated»; публичные эндпоинты помечены `[AllowAnonymous]`, `/health` и `/openapi` — тоже.
- Токены: сырой 32-байтный (base64url) в ссылке письма, в БД только SHA-256 хэш; TTL 24ч, single-use; выдача нового инвалидирует прежние. `resend` не раскрывает существование аккаунта.
- `.http` дополнен примерами auth-флоу; `appsettings.json` — секции `App`/`SMTP`.

Заметки:
- Пакеты: `Konscious.Security.Cryptography.Argon2` 1.3.1, `MailKit` 4.17.0.
- Верификационная ссылка (пока нет фронта) ведёт на backend `GET /auth/verify-email`; в Фазе 1b перенаправится через фронт.
- Тест-проект и business-flow тест перенесены в Фазу 1b (по решению разработчика).
- `dotnet build` (solution) — 0 warnings. Требуется проверка `docker compose up --build` + ручной прогон auth-флоу через MailPit на стороне разработчика.
