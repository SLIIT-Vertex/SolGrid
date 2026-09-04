# Prosumer Management Backend

## Purpose

Prosumer Management owns solar prosumer registration, profile maintenance, account activation, and account lifecycle. It is a backend FAT-service component: the API enforces validation, ownership, authorization, uniqueness, and lifecycle transitions. The web and Android clients are consumers of these contracts and do not decide account state.

## Architecture

| Layer | Responsibility |
| --- | --- |
| Domain | `Prosumer`, `ProsumerAccountStatus`, immutable NIC, and lifecycle transitions. It has no ASP.NET Core or MongoDB dependencies. |
| Application | DTOs, `IProsumerService`, `IProsumerRepository`, validation, ownership checks, and the focused Reservation read adapter. |
| Infrastructure | MongoDB document mapping, indexes, repository implementation, and the shared BCrypt password hasher/JWT token service. |
| API | Thin authenticated controllers, centralized ProblemDetails handling, and Swagger/OpenAPI metadata. |

## Entity

`Prosumer` stores:

| Field | Notes |
| --- | --- |
| `Nic` | Immutable normalized Sri Lankan NIC; MongoDB `_id` and stable business identifier. |
| `FirstName`, `LastName`, `Email`, `PhoneNumber` | Profile data. Email is normalized to lowercase. |
| `PasswordHash` | BCrypt hash only; never returned by API responses. |
| `Status` | `Pending`, `Active`, `DeactivationRequested`, or `Deactivated`. |
| `CreatedAt`, `UpdatedAt` | UTC audit timestamps. |

Accepted NIC shapes are the maintainable assignment-level formats: legacy nine digits followed by `V`/`X`, or a modern twelve-digit NIC. NIC is intentionally not editable after registration.

## Account Lifecycle

```text
Registration -> Pending --Backoffice activate--> Active
Active --Prosumer request--> DeactivationRequested --Backoffice deactivate--> Deactivated
Active --Backoffice deactivate---------------------> Deactivated
Pending --Backoffice deactivate--------------------> Deactivated
Deactivated --Backoffice reactivate----------------> Active
```

Illegal transitions return a `409 Conflict`. Only `Active` prosumers can obtain a new prosumer JWT. Profile access and profile updates also reject stale tokens after the account ceases to be active. A repeated deactivation request remains a lifecycle conflict.

## API Contract

| Endpoint | Access | Result |
| --- | --- | --- |
| `POST /api/v1/prosumers/register` | Anonymous | Registers a `Pending` account; returns `201`. |
| `POST /api/v1/prosumers/login` | Anonymous | Authenticates an active prosumer and returns a JWT. |
| `GET /api/v1/prosumers/me` | Authenticated active prosumer | Returns own profile. |
| `PUT /api/v1/prosumers/me` | Authenticated active prosumer | Updates first name, last name, email, and phone number only. |
| `PATCH /api/v1/prosumers/me/request-deactivation` | Authenticated prosumer | Records a deactivation request; returns `204`. |
| `GET /api/v1/prosumers` | Backoffice | Paginated search/filter list. Query: `searchText`, `status`, `pageNumber`, `pageSize`. |
| `GET /api/v1/prosumers/pending` | Backoffice | Pending activation queue. |
| `GET /api/v1/prosumers/{nic}` | Backoffice | One prosumer profile. |
| `PATCH /api/v1/prosumers/{nic}/activate` | Backoffice | `Pending` to `Active`; returns `204`. |
| `PATCH /api/v1/prosumers/{nic}/deactivate` | Backoffice | Allowed state to `Deactivated`; returns `204`. |
| `PATCH /api/v1/prosumers/{nic}/reactivate` | Backoffice | `Deactivated` to `Active`; returns `204`. |

The global exception handler emits `application/problem+json`: validation is `400`, unauthenticated is `401`, authorization/inactive-account is `403`, missing resources are `404`, and duplicate or lifecycle conflicts are `409`.

## Authorization Matrix

| Operation | Prosumer | GridOperator | Backoffice |
| --- | --- | --- | --- |
| Register/login | Yes, subject to lifecycle | N/A | N/A |
| Read/update own profile | Yes, active account only | No | No |
| Request own deactivation | Yes | No | No |
| List/read all prosumers | No | No | Yes |
| Activate/deactivate/reactivate | No | No | Yes |

JWT validation occurs before role policy evaluation. A prosumer token has only a stable subject (NIC), while web-user tokens include the server-issued role claim. Request bodies never determine a caller role or ownership.

## MongoDB Data Integrity

The `Prosumers` collection is the only prosumer profile source of truth. Its `_id` is the normalized NIC, which gives MongoDB-level NIC uniqueness without a redundant index. Additional indexes are:

| Index | Reason |
| --- | --- |
| `ux_prosumers_email` (unique) | Email identity and duplicate-email prevention. |
| `ix_prosumers_account_status` | Pending queue and account-status filtering. |

MongoDB-specific documents, filters, and indexes remain in Infrastructure. The repository translates duplicate-key races to application `ConflictException` values.

## Authentication

Registration passes the supplied password through the shared `IPasswordHasher` before persistence. Login compares it with the same abstraction and never returns or logs the hash. JWT configuration and signing secrets are supplied from configuration/environment secrets; no secrets are stored in source-controlled appsettings.

## Reservation Integration Contract

Reservation Management references a prosumer by the stable normalized NIC only. It uses `IReservationProsumerReadService.GetByIdAsync`, supplied by `ReservationProsumerReadService`, and receives only:

| Contract value | Meaning |
| --- | --- |
| `Id` | Stable normalized NIC. |
| `IsActive` | Booking eligibility at validation time. |

Reservation does not read the `Prosumers` Mongo collection, copy profile data, or receive `PasswordHash`. It must re-check `IsActive` when creating or modifying a reservation. This adapter is application-to-application only, so MongoDB details do not leak across components.

## Tests

The component test suite covers registration normalization, duplicate NIC/email handling, NIC/email validation, password hashing/no hash response leakage, initial pending state, own-profile lookup and updates, protected-field preservation, duplicate email update, lifecycle transitions, Backoffice restrictions, pending filtering, active-login eligibility, and stale-token inactive profile rejection. Infrastructure integration tests cover MongoDB CRUD and unique constraints when `SOLGRID_MONGO_TEST_CONNECTION_STRING` is configured.

## Known Limitations

- Password reset, email verification, and audit actor/reason fields are not assignment requirements and are not implemented.
- The MongoDB integration suite is opt-in so local test runs do not require a database; set `SOLGRID_MONGO_TEST_CONNECTION_STRING` to exercise it.
- Station and booking-slot reference implementations remain owned by Microgrid Node Management. Reservation can use the Prosumer contract above today.

## Viva Notes

- The NIC is both the assignment business identifier and MongoDB `_id`; it is immutable after registration.
- Lifecycle invariants live in the Domain entity; Application maps invalid transitions to consistent conflicts.
- A database unique index, not only an application pre-check, protects concurrent registrations.
- Application sees interfaces and snapshots; only Infrastructure imports MongoDB.
- The API is authoritative for status, ownership, and role checks. Client state is advisory only.
