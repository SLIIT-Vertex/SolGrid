# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## What this is

SolGrid is a SLIIT SE4040 (EAD) group assignment: a Smart Solar Microgrid Trading System. Three parts talk only through one REST API:

- `web-service/` — .NET (C#) Web API + MongoDB. **"Fat service"**: all business rules live here; clients are UI only.
- `web-app/` — React 19 + Vite + TypeScript (Backoffice and GridOperator roles).
- `mobile-app/` — native Android (Kotlin, Jetpack Compose, Retrofit) for Prosumers and Grid Operators.

Assignment constraints that shape code decisions: NIC is the prosumer primary key; reservations must be within 7 days and updates/cancels need ≥12 h notice; node deactivation is blocked while active reservations exist; only Backoffice can reactivate prosumers; QR codes are issued on approval and verified/completed server-side by operators. Every `.cs` file needs a comment header block and every method an inline comment at its start (the assignment says uncommented code is not marked). Android uses plain `SQLiteOpenHelper` (`core/storage/AppDatabase.kt`) only for local session state, not Room despite what the README says.

## Commands

Task runner is the root `Makefile` (needs Bash/GNU Make; on Windows use Git Bash or WSL). `make help` lists everything.

```bash
make env-init            # create .env with generated Mongo/JWT secrets (never commit .env)
make docker-up / docker-down / docker-logs   # Mongo + API (:5080) + web (:5173)
make secrets-init && make secrets-set        # load .env into .NET user-secrets for local runs
make run                 # run API locally (set ASPNETCORE_ENVIRONMENT=Development in .env)
make build | test | verify | format          # backend
make web-install && make web-dev             # React dev server (web-env generates Vite config from .env)
make web-build
make mobile-build | mobile-install | mobile-test   # Android (mobile-env generates ignored config from .env)
```

Single tests:

```bash
dotnet test web-service/tests/SolGrid.Application.Tests --filter "FullyQualifiedName~ReservationServiceTests"
cd web-app && npx vitest run src/components/layout/navItems.test.ts   # also: npm run lint, npm test
cd mobile-app && ./gradlew :app:testDebugUnitTest --tests "com.solgrid.mobile.core.qr.TransactionQrTest"
```

Mongo repository integration tests (`SolGrid.Infrastructure.Tests`) need a real DB: `make docker-test-mongo-up`, then `make test SOLGRID_MONGO_TEST_CONNECTION_STRING=mongodb://localhost:27018`.

Seed data: `scripts/seed-data.js`; optional dev seed users via `SEED_DEVELOPMENT_USERS=true` plus `BACKOFFICE_SEED_*` / `GRID_OPERATOR_SEED_*` in `.env`.

## Backend architecture

Clean Architecture across four projects in `web-service/src/`, dependencies pointing inward:

- `SolGrid.Domain` — entities/enums and entity-local rules (e.g. reservation status transitions, rejecting double completion).
- `SolGrid.Application` — DTOs, service interfaces, use cases, repository abstractions, grouped by feature (Auth, Users, Prosumers, SolarStations/slots, Reservations, Analytics). Cross-entity rules (7-day window, 12-hour notice, ownership, slot/station availability) are enforced **here**.
- `SolGrid.Infrastructure` — MongoDB repositories/indexes, BCrypt hashing, JWT. Duplicate-active-booking prevention is a repository query.
- `SolGrid.Api` — thin controllers (`/api/v1/...`), auth, DI, middleware.

Four test projects mirror these layers under `web-service/tests/`. Rule-by-rule enforcement table: `docs/energy-reservation-rules.md`; per-feature docs are in `docs/`; Postman collection at `docs/SolGrid.postman_collection.json`.

When adding a rule, put it in Application/Domain and never rely on client-side checks; clients may mirror validation only for UX.

## Client notes

- Web: feature folders under `web-app/src/features`, auth/role gating in `src/auth` (`roleAccess.ts`, `ProtectedRoute.tsx`), TanStack Query + axios for API calls.
- Android: `core/network` (Retrofit `ApiService`, repositories, `SessionStore`), `core/navigation`, `feature/{auth,prosumer,reservations,operator,microgrid,shared}` with a ViewModel per feature. Operator flow: `ScannerScreen` → server verification → `VerificationResultScreen`. API URL and Google Maps key come from ignored files generated from `.env` (`env.properties.example`, `secrets.properties.example`); `MOBILE_API_BASE_URL` must end with `/`.
- `core/mock/MockData.kt` exists on Android; real features must go through the API.

## Hosting

The assignment requires IIS hosting of the API (worth marks). Docker is only the local dev path. Use `scripts/deploy-iis.ps1` and see `docs/iis-deployment.md`; config is supplied via `MongoDb__*`, `Jwt__*`, `Cors__AllowedOrigins__N` environment variables.
