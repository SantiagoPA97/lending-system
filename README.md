# Ledgerline — Mini Lending Management System

A small lending management system: companies, credit facilities with generated repayment schedules, an immutable repayment ledger, unified search, a metrics dashboard, audit trail, role-based auth, and an AI portfolio assistant.

**Live demo:** https://app-production-d9f8.up.railway.app

- **Backend:** .NET 8 minimal APIs, EF Core, PostgreSQL
- **Frontend:** React 19 + TypeScript + Vite (TanStack Query, Tailwind, Recharts)
- **Deploy:** Railway (app container + Postgres), auto-deployed by GitHub Actions on push to `main` after tests pass

## Running locally

Prerequisites: .NET 8 SDK, Node 22, Docker.

```bash
# 1. Database
docker compose up -d postgres

# 2. API (from backend/) — http://localhost:5080, Swagger UI at /swagger
# (the launch profile sets Development env, migrations and demo seed)
dotnet run --project src/Lending.Api

# 3. Frontend (from frontend/) — http://localhost:5173, proxies /api and /auth to :5080
npm install
npm run dev
```

Local default auth is **Bypass** mode: the login screen shows a demo role picker (viewer / operator / admin) — no external identity provider needed.

### Environment variables

| Variable | Purpose |
|---|---|
| `DATABASE_URL` | Postgres URL (Railway-style); falls back to `ConnectionStrings:Default` (local compose defaults) |
| `MIGRATE_ON_STARTUP` | `true` to apply EF migrations at startup |
| `SEED_DEMO_DATA` | `true` to seed demo companies/facilities/repayments |
| `Auth__Mode` | `Bypass` (default) or `Auth0` |
| `Auth__Auth0__Domain` / `Auth__Auth0__ClientId` / `Auth__Auth0__ClientSecret` | Required when `Auth__Mode=Auth0` |
| `ANTHROPIC_API_KEY` | Enables the AI assistant; without it the endpoint reports `configured=false` and the UI shows a setup hint |
| `OTEL_EXPORTER_OTLP_ENDPOINT` | Enables OpenTelemetry traces/metrics via OTLP (e.g. Grafana Cloud); off when unset |

## Tests

| Suite | Count | Command |
|---|---|---|
| Domain (xUnit) — schedule math, rounding, allocation, state machines, Money | 122 | `cd backend && dotnet test tests/Lending.Domain.Tests` |
| API integration (WebApplicationFactory + Testcontainers PostgreSQL) | 29 | `cd backend && dotnet test tests/Lending.Api.Tests` (requires Docker) |
| Frontend (Vitest) — formatters, utils, component tests | 23 | `cd frontend && npm test` |

Integration tests run against a real Postgres container, so FTS/trigram search and `xmin` concurrency are tested against actual database behavior, not a fake.

## Authentication and roles

Auth uses the **BFF pattern**: the .NET app already serves the SPA from the same origin, so it acts as the OIDC **confidential client**. The code exchange happens server-side (client secret never reaches the browser); the browser holds only an HttpOnly, SameSite=Lax session cookie (`ledgerline.session`). CSRF protection = SameSite cookie + a required `X-Requested-With` header on all mutating `/api` requests. The frontend has zero auth SDK — plain fetch with cookies.

Two modes (`Auth:Mode`):

- **Bypass** (local default): `GET /auth/dev-login?role=viewer|operator|admin` issues a session for a fake user — the login screen is a role picker.
- **Auth0** (production): standard ASP.NET `OpenIdConnect` + cookie handlers, no vendor SDK. Google social connection enabled = real SSO. Roles arrive as a namespaced custom claim (`https://lending/roles`, added by an Auth0 Action) and are mapped to ASP.NET role claims.

### Roles and permissions

Authorization is **permission-based**: endpoints authorize against fine-grained permissions (`portfolio.read`, `portfolio.manage`, `repayments.record`, `repayments.reverse`, `facilities.close`), and roles are just bundles of permissions defined in one map (`RolePermissions` in `Features/Auth/Permissions.cs`). Permissions are resolved from the session's role claims on every request, so map changes apply to existing sessions instantly. Adding a role = one entry in that map + the matching Auth0 role. `/auth/me` returns both `roles` and the derived `permissions`, which the UI uses to hide/disable actions.

| Action | Permission | viewer | operator | admin |
|---|---|---|---|---|
| View everything (companies, facilities, schedules, dashboard, search, audit, assistant) | `portfolio.read` | yes | yes | yes |
| Create/edit companies and facilities, activate company/facility | `portfolio.manage` | – | yes | yes |
| Record repayments | `repayments.record` | – | yes | yes |
| Reverse repayments | `repayments.reverse` | – | – | yes |
| Deactivate company, cancel/default facility | `facilities.close` | – | – | yes |

Unauthorized responses are RFC 7807 problem JSON (401/403), never login redirects.

**Test users:** three reviewer accounts (one per role) are created in the Auth0 dashboard — credentials are provided with the submission. *(Placeholder: add credentials here.)*

## API surface

| Area | Endpoints |
|---|---|
| Companies | `GET/POST /api/companies`, `GET/PUT /api/companies/{id}`, `POST /api/companies/{id}/activate\|deactivate` |
| Facilities | `GET/POST /api/facilities`, `GET/PUT /api/facilities/{id}`, `POST /api/facilities/{id}/activate\|cancel\|default` |
| Schedules | `GET /api/facilities/{id}/schedule`, `POST /api/facilities/schedule-preview` (projection before creating) |
| Repayments | `GET/POST /api/facilities/{id}/repayments`, `POST /api/repayments/{id}/reverse` |
| Search | `GET /api/search?q=&status=&repaymentType=&currency=&minAmount=&maxAmount=` — unified companies + facilities, fuzzy match, filters, pagination |
| Dashboard | `GET /api/dashboard/metrics` |
| Audit | `GET /api/audit?entityType=&entityId=` |
| Assistant | `POST /api/assistant/query` |
| Auth | `GET /auth/login`, `GET /auth/me`, `POST /auth/logout` (+ `GET /auth/dev-login` in Bypass mode) |
| Health | `GET /health`, `GET /health/ready` |

All inputs validated with FluentValidation; all errors are RFC 7807 ProblemDetails (domain rule violations → 422 with explicit codes). Swagger UI in development.

## Domain rules (beyond CRUD)

- **Facility lifecycle:** `Draft → Active → Completed | Cancelled | Defaulted`. Draft is editable; activation locks terms and generates the schedule; only valid transitions allowed. Repaying to zero auto-completes the facility.
- **Schedules:** Bullet, Amortizing (annuity), and InterestOnly + balloon. Per-period rounding to 2dp with a final-period adjustment so the schedule sums exactly to principal.
- **Repayments:** immutable ledger — no edit/delete; corrections are reversal entries. Recording validates currency, Active status, and amount bounds, then allocates interest-first against oldest unpaid installments, remainder to principal — all in one transaction.
- **Concurrency:** `OutstandingPrincipal` is guarded by Postgres `xmin` optimistic concurrency.
- **Audit:** an EF Core `SaveChanges` interceptor captures entity, action, old/new values, user, and timestamp — zero per-endpoint audit code.

## Architecture decisions

- **Three projects, not full Clean Architecture.** Domain / Infrastructure / Api (feature folders), no separate Application layer or CQRS. Sized to the problem: the domain logic lives in tested domain classes, and an extra layer of indirection would add ceremony without protecting anything.
- **Postgres FTS + pg_trgm instead of Elasticsearch.** Search sits behind `ISearchService` (`backend/src/Lending.Infrastructure/Search/`) using a generated `tsvector` + GIN index and trigram fuzzy matching. An ES cluster for this data volume is unnecessary complexity; the swap path is implementing `ISearchService` against an ES client and switching the DI registration.
- **BFF cookies instead of SPA tokens.** OWASP-recommended for a single first-party client: no tokens in JavaScript, secret stays server-side. Trade-off: the API can't serve third-party clients without adding a JWT scheme — acceptable here since the SPA is the only client. The provider is swappable config (point `Authority` at Keycloak/Entra), not code.
- **HybridCache for dashboard metrics** (60s TTL, invalidated on writes). In-process is enough for one instance; the upgrade path is plugging Redis in as HybridCache's distributed backplane (Railway Redis) with no call-site changes.
- **Immutable repayment ledger with reversals** and **interest-first allocation** — standard lending bookkeeping: history is never rewritten, and corrections are themselves auditable entries.
- **Money:** `decimal(18,2)`, a `Money` value object (cross-currency operations throw), banker's rounding (`MidpointRounding.ToEven`) as the single rounding policy, monthly rate = annual/12 (30/360), rates expressed as percent.
- **Observability:** Serilog structured JSON to console (indexed by Railway's log explorer), correlation IDs on every response, health endpoints, and an env-gated OTLP exporter ready for Grafana Cloud.
- **Railway, single container.** Multi-stage Dockerfile builds the SPA and serves it from the API's `wwwroot` — one service, one URL, no CORS. Migrations and demo seed are env-gated startup flags.

## AI assistant

`POST /api/assistant/query` runs Claude in a tool-use loop over four **whitelisted read-only tools** (`search_facilities`, `get_company`, `get_portfolio_summary`, `get_upcoming_payments`) — never raw SQL, so the model can only read what the API already exposes. Role-gated like every other endpoint, with a graceful "not configured" fallback when `ANTHROPIC_API_KEY` is absent.

## Documented assumptions

- Facilities are **fully drawn at activation** — no tranches or partial drawdowns.
- **Monthly periods, 30/360 simple interest** (monthly rate = annual rate / 12).
- **Single currency per facility, no FX.** Aggregates are never summed across currencies; dashboard figures are grouped by currency.
- Repayments are allocated **interest-first** (oldest unpaid installments first), remainder to principal.
- **Reversal-only corrections** — repayment ledger entries are immutable.
- **Banker's rounding at 2 decimal places** (`MidpointRounding.ToEven`) everywhere.
- **Early full settlement waives remaining future interest** — when a repayment brings outstanding principal to zero, unpaid interest on not-yet-due installments is waived (not counted as received) and the schedule is marked settled. Reversing the settling repayment restores the waived amounts.
- **Repayments cannot be dated before the facility start date or in the future** — the system records payments that already happened.

## CI/CD

`.github/workflows/ci.yml`: on every push/PR, two parallel jobs — backend (`dotnet restore/build/test`, including Testcontainers integration tests on the runner's Docker) and frontend (`npm ci`, `npm test`, `npm run build`). On push to `main`, a `deploy` job (gated on both jobs passing) pushes to Railway via `railway up` using a `RAILWAY_TOKEN` secret. Railway runs the multi-stage Dockerfile build and swaps the app container; Postgres is a Railway plugin.

## Repository layout

```
backend/
  src/Lending.Domain/          entities, Money, ScheduleCalculator, domain rules
  src/Lending.Infrastructure/  EF Core, migrations, audit interceptor, search, seeder
  src/Lending.Api/             minimal API feature folders, auth, validation, assistant
  tests/Lending.Domain.Tests/  xUnit unit tests
  tests/Lending.Api.Tests/     integration tests (Testcontainers PostgreSQL)
frontend/                      Vite + React + TS SPA
docker-compose.yml             local Postgres
Dockerfile                     multi-stage: build SPA → publish API → serve SPA from wwwroot
.github/workflows/ci.yml       build + test + deploy
```

## Appendix: Auth0 setup

1. **Application:** create a **Regular Web Application** (the BFF is a confidential client — not a SPA app type).
   - Allowed Callback URLs: `http://localhost:5080/auth/callback`, `https://app-production-d9f8.up.railway.app/auth/callback`
   - Allowed Logout URLs: `http://localhost:5080/`, `https://app-production-d9f8.up.railway.app/`
2. **Roles:** under User Management → Roles, create `viewer`, `operator`, `admin` and assign them to users.
3. **Action:** add a post-login Action (Actions → Triggers → post-login) so roles reach the ID token:

   ```js
   exports.onExecutePostLogin = async (event, api) => {
     const roles = event.authorization?.roles ?? [];
     api.idToken.setCustomClaim('https://lending/roles', roles);
   };
   ```

4. **Google SSO:** enable the Google social connection (Authentication → Social) for the application.
5. **Configure the app:** set `Auth__Mode=Auth0`, `Auth__Auth0__Domain`, `Auth__Auth0__ClientId`, `Auth__Auth0__ClientSecret`.
6. **Test users:** create one user per role in the Auth0 dashboard (or assign roles to Google accounts after first login).
