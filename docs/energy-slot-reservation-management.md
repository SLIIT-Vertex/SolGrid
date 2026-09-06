# Energy Slot Reservation Management

## Purpose

This component owns the backend lifecycle for energy-slot reservations. It accepts reservations, enforces scheduling and ownership rules, supports operational review, and completes approved energy transfers through a server-verified QR token. It exposes APIs for the web dashboard and Android QR workflow; neither client is authoritative for reservation state.

## Architecture

| Layer | Responsibility |
| --- | --- |
| Domain | `EnergyReservation`, its lifecycle state, transition guards, audit fields, and concurrency version. |
| Application | Reservation use cases, authorization/ownership decisions, time rules, query contracts, and external read abstractions. |
| Infrastructure | MongoDB documents, indexes, query execution, optimistic concurrency, and cryptographic QR-token hashing. |
| API | JWT-protected, thin HTTP endpoints and centralized `ProblemDetails` error mapping. |

The component does not duplicate User, Prosumer, Station, or BookingSlot models. It reads only focused snapshots through `IReservationProsumerReadService`, `IReservationStationReadService`, and `IReservationBookingSlotReadService`.

## Reservation Entity

`EnergyReservation` stores its identifier, prosumer, station, booking slot, UTC schedule, status, lifecycle audit timestamps/users, QR-token metadata, and integer concurrency version. The raw QR token and its hash are never included in a standard reservation response.

```text
Pending -> Approved -> Completed
   |          |
   |          -> Cancelled
   -> Rejected
   -> Cancelled
```

Rescheduling is allowed only while `Pending` or `Approved`. Terminal states (`Rejected`, `Cancelled`, `Completed`) cannot be modified. Completion is a one-way operation.

## Endpoints

All endpoints require a Bearer JWT. Pages use `pageNumber` and `pageSize`, with a maximum page size of 100.

| Method | Route | Purpose | Access |
| --- | --- | --- | --- |
| POST | `/api/v1/reservations` | Create a pending reservation. | Authenticated owner; Backoffice can create for a supplied prosumer. |
| GET | `/api/v1/reservations` | Filtered operational list. | Backoffice, GridOperator |
| GET | `/api/v1/reservations/me` | Current caller's reservations. | Authenticated owner |
| GET | `/api/v1/reservations/{id}` | Get one reservation. | Owner, Backoffice, GridOperator |
| PUT | `/api/v1/reservations/{id}` | Reschedule an active reservation. | Owner, Backoffice, GridOperator |
| PATCH | `/api/v1/reservations/{id}/cancel` | Cancel without deleting history. | Owner, Backoffice, GridOperator |
| PATCH | `/api/v1/reservations/{id}/approve` | Approve a pending reservation. | Backoffice, GridOperator |
| PATCH | `/api/v1/reservations/{id}/reject` | Reject a pending reservation. | Backoffice, GridOperator |
| POST | `/api/v1/reservations/{id}/qr` | Issue/reissue an approved reservation QR payload. | Owner, Backoffice |
| POST | `/api/v1/reservations/verify-qr` | Verify an approved QR token. | Backoffice, GridOperator |
| POST | `/api/v1/reservations/{id}/complete` | Complete an approved energy transfer. | Backoffice, GridOperator |
| GET | `/api/v1/reservations/dashboard/summary` | Server-calculated dashboard counts. | Backoffice, GridOperator |
| GET | `/api/v1/reservations/current` | Current future pending/approved reservations. | Backoffice, GridOperator |
| GET | `/api/v1/reservations/pending` | Pending-reservation work queue. | Backoffice, GridOperator |
| GET | `/api/v1/reservations/history` | Terminal or past-scheduled reservation history. | Backoffice, GridOperator |

The general list and dashboard pages support useful identifier search plus `prosumerId`, `stationId`, `bookingSlotId`, `status` where applicable, `scheduledFrom`, `scheduledTo`, and pagination. Filtering and counting happen in MongoDB, never by downloading an entire collection to a client.

## Business Rules

| Rule | Backend behavior |
| --- | --- |
| Schedule window | A reservation cannot be in the past or more than seven days in the future. The seven-day boundary is inclusive. |
| Update notice | An update requires at least 12 hours before the current scheduled time. Exactly 12 hours is allowed. |
| Cancellation notice | A cancellation requires at least 12 hours before the current scheduled time. Exactly 12 hours is allowed. |
| Availability | The prosumer, station, and booking slot must exist; the prosumer/station must be active; the slot must be active, available, and belong to the station. |
| Slot conflict | Only one `Pending` or `Approved` reservation may occupy a booking slot. |
| Ownership | Client-supplied prosumer identifiers are ignored for normal callers; ownership is derived from the JWT subject. |
| Review | Only `Pending` reservations can be approved or rejected. Rejection requires a reason. |
| Completion | Only a currently `Approved` reservation with a valid token can complete once. |

## QR Flow

1. The owner or Backoffice requests a QR payload after approval.
2. The server generates a cryptographically random token, stores only its SHA-256 hash, and returns the raw token once with a 30-minute expiry.
3. Android renders/scans the payload, then sends it to the verification endpoint.
4. The API checks the token hash, expiry, and `Approved` state. It records verification only after success.
5. Completion rechecks the token and current state before setting `CompletedAt` and `CompletedBy`.

Issuing another QR payload replaces the stored hash and invalidates the earlier token. Reservation identifiers alone are not QR proof, and no signing secrets or raw token values are logged or persisted.

## MongoDB Integrity And Indexes

Collection: `EnergyReservations` (configurable through `MongoDb:EnergyReservationsCollectionName`). All persisted timestamps are UTC.

| Index | Fields | Purpose |
| --- | --- | --- |
| `ux_energy_reservations_active_slot` | `BookingSlotId`, partial where `IsActiveForBookingSlot = true`, unique | Atomically prevents duplicate pending/approved bookings, including concurrent creates/reschedules. |
| `ix_energy_reservations_prosumer_scheduled_at` | `ProsumerId`, `ScheduledAtUtc` | Owner history and schedule pages. |
| `ix_energy_reservations_station_status_scheduled_at` | `StationId`, `Status`, `ScheduledAtUtc` | Station filtering and operational workflows. |
| `ix_energy_reservations_status_scheduled_at` | `Status`, `ScheduledAtUtc` | Pending/current/history dashboard lists and counts. |

Every state mutation increments `Version`. Mongo replacements match both `_id` and the previous version, so simultaneous stale changes fail with `409 Conflict` rather than silently overwriting one another. The partial unique index is the final authority for the create/update slot race; the application-level availability check remains useful for a friendly early conflict response.

## Error Handling And Swagger

The global middleware returns `application/problem+json`: validation `400`, unauthenticated `401`, forbidden `403`, missing resources `404`, business/concurrency conflicts `409`, and sanitized unexpected failures `500`. Controllers declare success and error responses for Swagger/OpenAPI; protected endpoints include Bearer security metadata.

## Tests

Reservation tests cover domain transitions, create/update/cancel rules, query filters and ownership, dashboard views/counts, approval/rejection, QR verification, completion, duplicate completion, repository CRUD, active-slot uniqueness, and stale-update concurrency. Mongo repository tests require `SOLGRID_MONGO_TEST_CONNECTION_STRING`; otherwise their existing integration-test convention bypasses live Mongo work.

## Integration Notes

### Web Team

Use the dashboard endpoints for operational cards and lists. Do not compute pending or approved-future counts from a downloaded list. Send UTC ISO-8601 timestamps. Treat `409` as a refresh-and-retry signal, particularly after another operator changes a reservation.

### Android Team

Render the returned QR payload only after the reservation is approved. Scan and send the payload to `POST /api/v1/reservations/verify-qr`; complete the transfer through the completion endpoint with the same token. A scan is not final confirmation until the API responds successfully.

### Owning Components

User Management supplies JWT user id and role claims. Prosumer Management must provide active-prosumer lookup. Microgrid Node Management must provide active station and active/available booking-slot lookup, including station ownership of the slot.

## Known Limitations And Viva Notes

- Concrete adapters for the Prosumer, Station, and BookingSlot read contracts must be supplied by their owning components before a complete deployed workflow is possible.
- Identifier search is server-side and paged; it is intentionally not a full-text search engine. Add a dedicated search design only if actual UI requirements demand it.
- The unique active-slot index may surface legacy duplicate data at startup. That is deliberate: resolve duplicate active records before enabling the invariant.
- The concise viva explanation is: Domain protects legal state transitions; Application validates cross-component business rules; MongoDB has the final duplicate-booking guarantee; JWT claims provide identity/role; QR tokens are random, hashed, short-lived, and revalidated at completion.
