# SolGrid

SolGrid is an enterprise energy-management platform for managing solar prosumers, microgrid nodes, energy booking slots, and energy slot reservations. The system provides web and Android clients backed by a .NET API and MongoDB.

## Components

| Component | Purpose |
| --- | --- |
| User Management | Backoffice and GridOperator user accounts, authentication, JWT-based authorization, and account administration. |
| Prosumer Management | Prosumer registration, profile management, account activation, deactivation, and reactivation workflows. |
| Microgrid Node Management | Solar station and booking-slot administration, schedules, availability, and nearby-node queries. |
| Energy Slot Reservation Management | Reservation creation, update, cancellation, approval, rejection, QR verification, transaction completion, history, and dashboard summaries. |

## Technology Stack

- Backend: .NET 10, ASP.NET Core, MongoDB, BCrypt, JWT Bearer authentication
- Web application: React, Vite, TypeScript
- Android application: Kotlin, Jetpack Compose, Retrofit, Room/SQLite, Google Maps
- Local deployment: Docker Compose

## Architecture

The backend follows Clean Architecture and keeps the web API, business use cases, domain rules, and MongoDB implementation in separate projects.

```text
web-service/
  src/
    SolGrid.Domain/          Entities, enums, and entity-local rules
    SolGrid.Application/     DTOs, service contracts, use cases, and abstractions
    SolGrid.Infrastructure/  MongoDB, password hashing, JWT, and repository implementations
    SolGrid.Api/             Controllers, authentication, dependency injection, and middleware
  tests/                     Unit, API, and MongoDB integration tests
web-app/                     React web application
mobile-app/                  Android application
```

## Prerequisites

- Docker Desktop with Docker Compose
- Node.js 22.12 or later and npm
- .NET SDK required by the backend projects
- Android Studio, Android SDK, and JDK 17 for the mobile application
- GNU Make and Bash are required for the project task commands. On Windows, use WSL or Git Bash.

## Quick Start With Docker

1. Create the local environment file:

   ```bash
   make env-init
   ```

2. Edit `.env` and provide at least the required MongoDB and JWT values:

   ```dotenv
   MONGO_ROOT_PASSWORD=use-a-strong-local-password
   JWT_SIGNING_KEY=use-a-long-random-signing-key
   ```

   `JWT_SIGNING_KEY` must be a long, randomly generated secret. Do not commit `.env`.

3. Start MongoDB, the backend, and the web application:

   ```bash
   make docker-up
   ```

4. Check service status and logs:

   ```bash
   make docker-ps
   make docker-logs
   ```

The default local addresses are:

| Service | Address |
| --- | --- |
| Backend API | `http://localhost:5080` |
| Backend health check | `http://localhost:5080/health` |
| OpenAPI document | `http://localhost:5080/openapi/v1.json` |
| Web application | `http://localhost:5173` |
| MongoDB | `mongodb://localhost:27017` |

The host port is configurable through `BACKEND_PORT` in `.env`. Port `5000` is not used by the Docker setup.

Stop the local stack with:

```bash
make docker-down
```

## Local Backend Development

For local API development, set `ASPNETCORE_ENVIRONMENT=Development` in `.env`, then initialize and load .NET user secrets:

```bash
make env-init
make secrets-init
make secrets-set
make run
```

Useful checks:

```bash
make health
make openapi
make login-backoffice
make login-grid-operator
```

Development seed accounts are optional. To enable them, set `SEED_DEVELOPMENT_USERS=true` and provide the `BACKOFFICE_SEED_*` and `GRID_OPERATOR_SEED_*` values in `.env`. These are local-development credentials and must not be used in production.

## Configuration

Configuration is read in this order: command-line overrides, environment variables, `.env`, then built-in defaults. `.env` is ignored by Git.

| Setting | Purpose |
| --- | --- |
| `MONGO_ROOT_USERNAME` / `MONGO_ROOT_PASSWORD` | MongoDB administrator credentials used by Docker Compose. |
| `MONGO_DATABASE` | MongoDB database name. Default: `SolGrid`. |
| `BACKEND_PORT` | Backend host port. Default: `5080`. |
| `WEB_APP_PORT` | Web application host port. Default: `5173`. |
| `JWT_ISSUER` / `JWT_AUDIENCE` | JWT issuer and audience validation values. |
| `JWT_SIGNING_KEY` | Required JWT signing secret. |
| `JWT_EXPIRES_MINUTES` | Access-token lifetime. Default: `60`. |
| `WEB_APP_ORIGIN` | Allowed local web origin for CORS. |
| `MOBILE_API_BASE_URL` | Android API URL. It must end with `/`. |
| `MOBILE_MAPS_API_KEY` | Google Maps Android key, copied only to ignored mobile build configuration. |

## Running the Web Application

For local web development outside Docker:

```bash
make web-install
make web-dev
```

`make web-dev` generates the ignored `web-app/.env.local` file from the root `.env`. Set `VITE_API_BASE_URL` and `VITE_GOOGLE_MAPS_API_KEY` in the root file before starting Vite. The API value must be the API root, such as `https://solgrid-production-7e42.up.railway.app/`, not `https://solgrid-production-7e42.up.railway.app/health`. The Maps key must be a Google Maps JavaScript API key restricted to the web application's browser origins, for example `http://localhost:5173`; an Android-restricted key will not work in the web application. Restart Vite after changing either setting.

## Running the Android Application

The Android application receives only the API URL and Google Maps key through the ignored `mobile-app/env.properties` file.

1. Set the following values in the root `.env`:

   ```dotenv
   MOBILE_API_BASE_URL=http://10.0.2.2:5080/
   MOBILE_MAPS_API_KEY=your-google-maps-android-key
   ```

   Use `http://10.0.2.2:5080/` for the Android emulator. For a physical device, use the computer's LAN address, for example `http://192.168.1.2:5080/`.

2. Generate mobile-local configuration and build:

   ```bash
   make mobile-env
   make mobile-build
   ```

3. Install on a connected emulator or device:

   ```bash
   make mobile-install
   ```

The debug APK is produced at `mobile-app/app/build/outputs/apk/debug/app-debug.apk`.

Restrict the Google Maps key to the Android application package and signing certificate in Google Cloud Console. Never place the key in source code.

## API Overview

All API routes use the `/api/v1` prefix. Protected routes require a valid bearer token:

```http
Authorization: Bearer <access-token>
```

| Area | Example endpoints |
| --- | --- |
| Authentication | `POST /api/v1/auth/login`, `GET /api/v1/auth/me` |
| Users | `POST /api/v1/users`, `GET /api/v1/users`, `PATCH /api/v1/users/{id}/deactivate` |
| Prosumers | `POST /api/v1/prosumers/register`, `GET /api/v1/prosumers/me`, `PATCH /api/v1/prosumers/{nic}/activate` |
| Stations and slots | `GET /api/v1/stations/nearby`, `POST /api/v1/stations/{stationId}/slots`, `PATCH /api/v1/slots/{id}/activate` |
| Reservations | `POST /api/v1/reservations`, `GET /api/v1/reservations/me`, `PATCH /api/v1/reservations/{id}/cancel`, `POST /api/v1/reservations/verify-qr` |
| Analytics | `GET /api/v1/analytics` |

The current API contract is available from the OpenAPI document at `/openapi/v1.json`.

## Key Business Rules

- A reservation must be scheduled within seven days and cannot be scheduled in the past.
- Reservation updates and cancellations require at least twelve hours' notice.
- A booking slot cannot have more than one active reservation at a time.
- Only active prosumers can make reservations.
- Stations and slots must be active and available before they can be reserved.
- Reservation ownership is derived from authenticated server-side claims; the client cannot choose another prosumer's reservation.
- Backoffice users manage user, prosumer, station, and reservation administration.
- GridOperators can manage booking-slot availability, but cannot administer user accounts or prosumer lifecycle operations.
- Approved reservations use a server-verifiable QR transaction token. Completion is server-authoritative and cannot be repeated.

## Testing and Quality Checks

Run backend build and tests:

```bash
make restore
make verify
```

Run MongoDB-backed integration tests when needed:

```bash
make docker-test-mongo-up
make test SOLGRID_MONGO_TEST_CONNECTION_STRING=mongodb://localhost:27018
make docker-test-mongo-down
```

Run Android unit tests:

```bash
make mobile-test
```

## Documentation

Detailed component documentation is available in the [`docs`](docs) directory:

- [User Management](docs/user-management-backend.md)
- [Prosumer Management](docs/prosumer-management.md)
- [Microgrid Node Management](docs/microgrid-node-management.md)
- [Energy Slot Reservation Management](docs/energy-slot-reservation-management.md)
- [Reservation Rules](docs/energy-reservation-rules.md)
- [QR Transactions](docs/qr-transactions.md)
- [Development Guide](docs/development.md)

## Team Contributions

| Member | Primary component ownership |
| --- | --- |
| Godage G D K G | Microgrid Node Management |
| Bawanthi K D R | User Management and authentication foundation |
| Gunasekara H N | Prosumer Management |
| Dilshan Yapa S Y C T | Energy Slot Reservation Management |

Update this table with student registration numbers and any agreed shared contributions before final submission.

## Submission Links

- Git repository: `TODO: add the repository URL`
- Demonstration video: `TODO: add the demonstration video URL`
