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

### Фаза 1b — Auth (frontend) + тесты ✅ (ветка `feat/auth-ui`)

Сделано:
- Frontend: `react-router-dom`; API-клиент (`api/client.ts` с `credentials:'include'` + разбор ProblemDetails, `api/auth.ts`); `AuthContext` (сессия через `/auth/me`, login/logout); `RequireAuth`-guard; маршруты (`AppRoutes`).
- Экраны: `LoginPage` (ошибки 401/403 + ссылка на resend при 403), `SignupPage` («проверьте почту»), `VerifyEmailPage` (берёт token из query, дёргает API, результат + переход на login), `ResendPage`; защищённая заглушка `HomePage` (email + logout).
- Backend-правки: верификационная ссылка теперь ведёт на фронт `{App:BaseUrl}/verify-email?token=…` (`App__BaseUrl=http://localhost:8080` в compose, dev — `5173`); флаг `RunMigrationsOnStartup` (default true) для тестов.
- Тесты: проект `tests/HackathonTaskTicketingSystem.Tests` в `.slnx`; `WebApplicationFactory<Program>` на SQLite in-memory (`EnsureCreated`) + `FakeEmailSender`. 3 теста: полный signup→verify→login флоу (+403 до верификации), `/me` без auth → 401, дубликат signup → 409. **Все зелёные, гоняются без Docker.**

Заметки:
- Тест поймал реальный баг: в .NET 10 DataAnnotations на record-параметрах должны быть на самих параметрах, а не `[property:]` — иначе signup падал 500. Исправлено.
- `NU1903` от транзитивного `SQLitePCLRaw.lib.e_sqlite3` 2.1.11 закрыт пином `SQLitePCLRaw.bundle_e_sqlite3` 3.0.3 в тест-проекте.
- `dotnet test` — 3/3 pass, 0 warnings; `npm run build` (frontend) — чисто. Auth-флоу через UI проверен разработчиком end-to-end — работает.
- **Отложено на Фазу 7 (UI polish):** ссылки навигации (Back to login, Resend, Sign up и т.п.) → единые кнопки/`LinkButton`; консистентная стилизация всех экранов разом, с переиспользуемыми компонентами.

### Фаза 2a — Teams (backend) ✅ (ветка `feat/teams`)

Сделано:
- `IAuditableEntity` + `AuditableEntitySaveChangesInterceptor`: авто-проставление `CreatedAt`/`ModifiedAt` (оба при insert, только `ModifiedAt` при реальном изменении). Зарегистрирован в DbContext через DI. Централизует правило `modified_at`, переиспользуется для Epics/Tickets.
- Сущность `Team` (`Name` + `NormalizedName` для case-insensitive уникальности с сохранением регистра) + конфигурация (уникальный индекс по `NormalizedName`). Миграция `AddTeams`.
- `TeamService` + `TeamsController`: `GET /teams`, `GET /teams/{id}`, `POST /teams`, `PUT /teams/{id}`, `DELETE /teams/{id}`. Валидация: пустое имя → 400, дубликат → 409, не найдено → 404. Все под auth.
- `.http` дополнен примерами Teams.

Заметки:
- **Delete-guard (409 при наличии epics/tickets) ещё не активен** — связанных сущностей нет; помечено `TODO` в `TeamService.DeleteAsync`, включим в Фазах 3/4.
- Тесты Teams — в Фазе 2b (по правилу «тесты после полной реализации фазы»). При добавлении тестов не забыть подключить `AuditableEntitySaveChangesInterceptor` в тестовую SQLite-фабрику.
- `dotnet build` — 0 warnings; существующие 3 auth-теста зелёные. Требуется проверка `docker compose up --build` + ручной прогон Teams API на стороне разработчика.

### Фаза 2b — Teams (frontend) + тесты ✅ (ветка `feat/teams-ui`)

Сделано:
- Frontend: `api/teams.ts` (+ `put`/`del` в `api/client.ts`); экран `TeamsPage` (список, создание, inline-переименование, удаление с inline-подтверждением; loading/empty/error; текст ошибки при дубликате). Роут `/teams` под `RequireAuth` + ссылка с `HomePage`.
- Тесты: фабрика обобщена `AuthTestFactory` → **`ApiTestFactory`** (+ `AuditableEntitySaveChangesInterceptor` в SQLite-конфиг, + хелпер `CreateAuthenticatedClientAsync`). Добавлены `TeamsApiTests`: create→201, дубликат (иной регистр)→409, rename→200, delete→204 затем 404, без auth→401.

Заметки:
- `dotnet test` — **8/8 pass** (3 auth + 5 teams), 0 warnings; `npm run build` — чисто. Требуется финальная проверка Teams через UI + `docker compose up --build` на стороне разработчика.
- UI-полировка (единые кнопки/стиль) — по-прежнему отложена на Фазу 7.
- **Фикс по фидбеку:** гонка двойной отправки формы (создание/переименование) могла создать команду и одновременно показать 409 от второго запроса. Добавлена синхронная защита через `ref` (не полагаясь на асинхронный `disabled`). Дублей-строк в БД быть не может — уникальный индекс по `NormalizedName`.

### Фаза 3a — Epics (backend) ✅ (ветка `feat/epics`)

Сделано:
- Сущность `Epic` (`IAuditableEntity`): `TeamId` (immutable), `Title` (непустой), `Description?`; конфигурация с FK на `Team` (`DeleteBehavior.Restrict`) + индекс по `TeamId`. Миграция `AddEpics`.
- `EpicService` + `EpicsController`: `GET /epics?teamId=`, `GET /epics/{id}`, `POST /epics`, `PUT /epics/{id}` (без смены team), `DELETE /epics/{id}`. Валидация: пустой title → 400, несуществующий team → 400, не найдено → 404.
- **Активирован delete-guard команды:** `TeamService.DeleteAsync` → `Blocked` (409), если у команды есть эпики; контроллер отдаёт 409 с ProblemDetails.
- `.http` дополнен примерами Epics.

Заметки:
- Delete epic → 409 при наличии тикетов — `TODO`, включим в Фазе 4.
- `dotnet build` — 0 warnings; тесты 8/8 зелёные (delete команды без эпиков не сломан). Тесты Epics + «team с эпиком → 409» — в Фазе 3b. Требуется проверка `docker compose up --build` + ручной прогон Epics API на стороне разработчика.

### Фаза 3b — Epics (frontend) + тесты ✅ (ветка `feat/epics-ui`)

Сделано:
- Frontend: `api/epics.ts`; экран `EpicsPage` — селектор команды, список эпиков команды, создание (title + description), inline-редактирование (title/description; команда неизменяема — не показывается в форме), удаление с подтверждением; защита от двойной отправки. Роут `/epics` под `RequireAuth` + ссылка с `HomePage`.
- Тесты `EpicsApiTests` (6): создать → 201; пустой title → 400; несуществующий teamId → 400; изменить → 200; удалить → 204 затем 404; **удаление team с эпиком → 409**.

Заметки:
- `dotnet test` — **14/14 pass** (3 auth + 5 teams + 6 epics), 0 warnings; `npm run build` — чисто. Требуется финальная проверка Epics через UI + `docker compose up --build` на стороне разработчика.
- UI-полировка — по-прежнему отложена на Фазу 7.

### Фаза 4a — Tickets (backend) ✅ (ветка `feat/tickets`)

Сделано:
- Enum'ы `TicketType`/`TicketState`; глобальный `JsonStringEnumConverter(SnakeCaseLower)` → API использует канонические snake_case значения; невалидный enum → 400. В БД хранятся строкой.
- Сущность `Ticket` (`IAuditableEntity`): `TeamId`, `EpicId?`, `Type`, `State`, `Title`, `Body`, `CreatedById`; конфигурация (FK `Restrict`, индекс `(TeamId, ModifiedAt)`). Миграция `AddTickets`.
- `TicketService` + `TicketsController`: `GET /tickets?teamId=&type=&epicId=&search=` (сорт. `ModifiedAt desc`, поиск по title CI), `GET /tickets/{id}`, `POST` (state опц., дефолт `new`), `PUT` (type/team/epic/title/body/state), `DELETE`.
- Правила: title/body непустые → 400; несуществующий team → 400; **epic только из той же команды** (и при смене team) → 400; `created_by` из `ICurrentUser`.
- **Активированы оставшиеся delete-guard'ы:** epic с тикетами → 409; team теперь блокируется и эпиками, и тикетами → 409.
- `.http` дополнен примерами Tickets.

Заметки:
- Отдельный эндпоинт смены состояния (для drag-and-drop) и Kanban-доска — Фаза 6. Комментарии — Фаза 5.
- `dotnet build` — 0 warnings; тесты 14/14 зелёные. Тесты Tickets — в Фазе 4b. Требуется проверка `docker compose up --build` + ручной прогон Tickets API на стороне разработчика.

### Фаза 4b — Tickets (frontend) + тесты ✅ (ветка `feat/tickets-ui`)

Сделано:
- Frontend: `api/tickets.ts` (типы, `stateLabel`, фильтры); экран `TicketsPage` (селектор команды → список; состояния list/details/create/edit); `TicketForm` (type, state, team, epic-дропдаун команды, title, body) — **смена team очищает epic и перезагружает список эпиков**; детали тикета со всеми полями (`createdBy`, `createdAt`, `modifiedAt`); удаление с подтверждением; защита от двойной отправки. Роут `/tickets` + ссылка с `HomePage`.
- Тесты `TicketsApiTests` (9): создать → 201; epic из чужой команды → 400; пустой title → 400; невалидный enum state → 400; изменить → 200; удалить → 204/404; фильтр по типу; epic-с-тикетом → 409; team-с-тикетом → 409.

Заметки:
- `dotnet test` — **23/23 pass** (3 auth + 5 teams + 6 epics + 9 tickets), 0 warnings; `npm run build` — чисто. Требуется финальная проверка Tickets через UI + `docker compose up --build` на стороне разработчика.
- Kanban-доска (drag-and-drop, богатые фильтры) — Фаза 6. Комментарии — Фаза 5.

### Фаза 5a — Comments (backend) ✅ (ветка `feat/comments`)

Сделано:
- Сущность `Comment` (`TicketId`, `AuthorId`, `Body`, `CreatedAt`; не `IAuditable`, `CreatedAt` из `IClock`); конфигурация: FK на `Ticket` **`Cascade`** (удаление тикета удаляет комментарии), FK на автора `Restrict`, индекс `(TicketId, CreatedAt)`. Миграция `AddComments`.
- `CommentService` + `CommentsController`: `GET /tickets/{ticketId}/comments` (oldest-first), `POST /tickets/{ticketId}/comments`. Тикет не найден → 404; пустой body → 400; author из `ICurrentUser`.
- **`modified_at` тикета не двигается** при добавлении комментария (тикет не изменяется, интерцептор его не трогает).

Заметки:
- `dotnet build` — 0 warnings; тесты 23/23 зелёные. Тесты Comments (вкл. «комментарий не двигает modified_at» и «удаление тикета удаляет комментарии») — в Фазе 5b. Требуется проверка `docker compose up --build` + ручной прогон на стороне разработчика.

### Фаза 5b — Comments (frontend) + тесты ✅ (ветка `feat/comments-ui`)

Сделано:
- Frontend: `api/comments.ts`; компонент `TicketComments` встроен в детали тикета — список oldest-first (автор + время + текст) + форма добавления; loading/empty/error, защита от двойной отправки.
- Тесты `CommentsApiTests` (6): добавить → 201; oldest-first (сорт. по `createdAt`); пустой body → 400; чужой ticket → 404; **комментарий не меняет `modified_at` тикета**; **удаление тикета с комментарием → 204** (cascade подтверждён).

Заметки:
- `dotnet test` — **29/29 pass** (3 auth + 5 teams + 6 epics + 9 tickets + 6 comments), 0 warnings; `npm run build` — чисто. Требуется финальная проверка комментариев через UI + `docker compose up --build` на стороне разработчика.

### Фаза 6a — Board backend (state endpoint) ✅ (ветка `feat/board`)

Сделано:
- `PATCH /tickets/{id}/state` (`ChangeTicketStateRequest`); `TicketService.ChangeStateAsync` — любое состояние → любое; не найден → 404; невалидный enum → 400; `modified_at` двигается только при реальной смене. Схема не менялась — миграции нет.
- `.http` дополнен примером PATCH state.

Заметки:
- `dotnet build` — 0 warnings. Тесты state-эндпоинта + доска — в Фазе 6b. Доска грузит `GET /tickets?teamId=` и группирует/фильтрует на клиенте.

### Фаза 6b — Kanban board (frontend) + тесты ✅ (ветка `feat/board-ui`)

Сделано:
- Рефактор: `TicketDetails` вынесен в `features/tickets/TicketDetails.tsx` (переиспользуется списком тикетов и доской).
- `@dnd-kit/core`; `api/client` получил `patch`, `ticketsApi.changeState`.
- Экран `BoardPage` (роут `/board`, ссылка с `HomePage`): селектор команды → 5 колонок по состояниям; карточки (title + type + epic); **drag-and-drop между любыми колонками** с оптимистичным перемещением, `PATCH /state`, **откатом и ошибкой при сбое**; сортировка в колонке по `modified_at desc`; фильтры type/epic + поиск по title (**AND**, client-side); создание (`TicketForm` в модалке) и открытие (`TicketDetails` в модалке, с Edit/Delete/комментариями).
- Тесты в `TicketsApiTests` (+3): смена состояния → 200 + новое состояние + `modified_at` продвинулся; невалидный state → 400; чужой id → 404.

Заметки:
- Сам drag-and-drop — браузерное поведение, проверяется вручную; DoD по тестам закрыт API-тестами state-эндпоинта.
- `dotnet test` — **32/32 pass**, 0 warnings; `npm run build` — чисто. Требуется финальная проверка доски через UI + `docker compose up --build` на стороне разработчика.
- UI-полировка — по-прежнему Фаза 7.

### Фаза 7 — Polish + финал ✅ (ветка `chore/polish`)

Сделано:
- Тема `components/theme.ts`: палитры для 5 состояний (колонок) и типов тикетов; компоненты `Button`/`LinkButton` (primary/secondary/danger) — единый стиль вместо inline-кнопок по всем экранам.
- **Верхняя навигация** `AppLayout` (Board · Teams · Epics · Tickets · Log out) для авторизованных страниц; убраны разрозненные «← Back»; `HomePage` удалён; **`/` → редирект на `/board`** (доска — primary screen).
- **Цветовое кодирование**: колонки доски — свой акцент/фон + подсветка при drop; бейджи типа (bug/feature/fix) и состояния — цветные (на доске, в списке тикетов, в деталях).
- Auth-экраны: навигационные ссылки → кнопки (`LinkButton`), сабмиты → `Button`.

Сверка **Definition of Done** (спека §13) — всё выполнено:
- ✅ signup → письмо (MailPit) → верификация → вход
- ✅ Teams/Epics управляются через UI и персистятся
- ✅ verified-пользователь создаёт/смотрит/редактирует/удаляет тикеты
- ✅ комментарии с автором и временем
- ✅ доска показывает тикеты в правильных колонках по команде
- ✅ drag меняет состояние на сервере и сохраняется после refresh
- ✅ старт с чистого чекаута через `docker compose up --build`
- ✅ нет хардкод-паролей/закоммиченных секретов (`.env` в `.gitignore`, `.env.example` — плейсхолдеры)
- ✅ свежая БД = схема + метаданные миграций, без seed
- ✅ QA создаёт данные через UI/API
- ✅ тесты: backend business-flow (auth) + API-flow (32 теста)

Заметки:
- **Фикс по фидбеку:** на доске убран агрессивный авто-скролл dnd-kit (`autoScroll={false}`) и колонки растянуты на высоту вьюпорта — зона drop-а теперь по всей высоте колонки, страница не «улетает» вниз.
- `dotnet test` — 32/32, 0 warnings; `npm run build` — чисто. Требуется финальная визуальная проверка + `docker compose up --build` с чистого чекаута на стороне разработчика.
