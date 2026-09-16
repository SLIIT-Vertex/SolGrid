# User Management Backend

## Overview

User Management covers web API accounts for the SolGrid Backoffice and GridOperator roles. It does not cover Prosumer Management.

The backend is the authoritative source for user validation, password hashing, account status, authentication, and role authorization. React and Android clients should call the API and must not access MongoDB directly.

## Roles

| Role | Purpose |
| --- | --- |
| Backoffice | Administrative web user. Can manage Backoffice and GridOperator web users. |
| GridOperator | Operational web user. Can authenticate, but cannot administer web users. |

## Login Flow

1. Client sends `POST /api/v1/auth/login` with email and password.
2. Application normalizes email and finds the user through `IUserRepository`.
3. Password is verified through `IPasswordHasher`.
4. Inactive accounts are rejected.
5. Infrastructure issues a signed JWT through `ITokenService`.
6. Client sends the token as `Authorization: Bearer <token>` on protected requests.

## JWT Claims

JWTs intentionally contain only the claims required for authentication and authorization:

| Claim | Value |
| --- | --- |
| `ClaimTypes.NameIdentifier` | Stable user id |
| `ClaimTypes.Role` | `Backoffice` or `GridOperator` |

Tokens do not include password hashes or unnecessary personal data.

## Endpoints

| Method | Path | Authorization | Success |
| --- | --- | --- | --- |
| POST | `/api/v1/auth/login` | Anonymous | `200 OK` |
| GET | `/api/v1/auth/me` | Any authenticated user | `200 OK` |
| POST | `/api/v1/users` | Backoffice | `201 Created` |
| GET | `/api/v1/users` | Backoffice | `200 OK` |
| GET | `/api/v1/users/{id}` | Backoffice | `200 OK` |
| PUT | `/api/v1/users/{id}` | Backoffice | `200 OK` |
| PATCH | `/api/v1/users/{id}/deactivate` | Backoffice | `204 No Content` |
| PATCH | `/api/v1/users/{id}/reactivate` | Backoffice | `204 No Content` |

`GET /api/v1/users` supports:

| Query | Type | Notes |
| --- | --- | --- |
| `searchText` | string | Matches first name, last name, or email. |
| `role` | enum | `Backoffice` or `GridOperator`. |
| `status` | enum | `Active` or `Inactive`. |
| `pageNumber` | integer | Must be greater than zero. |
| `pageSize` | integer | Must be greater than zero. Repository caps excessive page sizes. |

## Configuration

| Key | Required | Notes |
| --- | --- | --- |
| `MongoDb:ConnectionString` | Yes | Use environment-specific configuration outside source control for deployed environments. |
| `MongoDb:DatabaseName` | Yes | MongoDB database name. |
| `MongoDb:UsersCollectionName` | Yes | Defaults to `Users` in local config. |
| `MongoDb:InitializeOnStartup` | Yes | When true, the API creates required indexes and optional seed users at startup. |
| `MongoDb:SeedDevelopmentUsers` | No | Enables optional development seed users only when explicitly true. |
| `MongoDb:BackofficeSeedUser:*` | Development only | Configure through local secrets/environment variables if needed. |
| `MongoDb:GridOperatorSeedUser:*` | Development only | Configure through local secrets/environment variables if needed. |
| `Jwt:Issuer` | Yes | Must match issuer validation. |
| `Jwt:Audience` | Yes | Must match audience validation. |
| `Jwt:SigningKey` | Yes | Must be supplied from environment/secrets and be at least 32 bytes. |
| `Jwt:ExpiresMinutes` | Yes | Positive token lifetime in minutes. |
| `Cors:AllowedOrigins` | Browser clients | Include React development and deployed web origins. |

`appsettings.json` intentionally does not contain a production JWT signing key.

These commands run in Windows PowerShell/Command Prompt, macOS, and Linux with Node.js 22.12+ installed. See [development commands](development.md) for prerequisites and all tasks.

For local development, create `.env` with generated local-only secrets:

```bash
node scripts/tasks.mjs env-init
```

For `dotnet run`, store those `.env` values in .NET user-secrets:

```bash
node scripts/tasks.mjs secrets-set
node scripts/tasks.mjs run
```

The API project has a `UserSecretsId`, so `ASPNETCORE_ENVIRONMENT=Development` loads these local secrets without committing them.

For Docker Compose, use the same `.env` file:

```bash
node scripts/tasks.mjs docker-up
```

The backend container reads configuration through environment variables such as `Jwt__SigningKey` and `MongoDb__ConnectionString`. Docker Compose requires `JWT_SIGNING_KEY` and `MONGO_ROOT_PASSWORD` to be present in `.env`; real secrets must never be committed.

## Seeded Development Accounts

Development seed accounts are disabled by default. When enabled, each configured seed user is created only if its email does not already exist. Seed passwords are hashed through the shared password hashing abstraction before persistence.

Do not commit real seed credentials or production secrets.

## Integration Notes

React:

- Store the JWT only in the chosen application session mechanism.
- Send `Authorization: Bearer <token>` for protected API calls.
- Use `401` to redirect to login and `403` to show an access-denied state.
- Do not hide user administration only in the UI; the API enforces Backoffice authorization.

Android:

- Prosumer Management is not part of this backend component yet.
- Android clients should use the login endpoint only if a Backoffice/GridOperator web-user mode is intentionally needed later.
- Do not persist passwords. If tokens are stored, use platform-secure storage.

## Persistence Notes

The `Users` collection stores normalized email, BCrypt password hash, role, account status, and UTC timestamps. MongoDB indexes enforce unique email and support role/status filters.

MongoDB repository integration tests are designed to run against a real MongoDB instance when `SOLGRID_MONGO_TEST_CONNECTION_STRING` is set. A compose file is included for that workflow:

```bash
docker compose -f docker-compose.test.yml up -d
SOLGRID_MONGO_TEST_CONNECTION_STRING=mongodb://localhost:27018 \
  dotnet test web-service/tests/SolGrid.Infrastructure.Tests/SolGrid.Infrastructure.Tests.csproj
docker compose -f docker-compose.test.yml down
```
