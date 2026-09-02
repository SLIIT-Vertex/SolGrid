# Energy Slot Reservation Management Backend

## Current Scope

Phase 1 added the Energy Slot Reservation Management backend foundation:

- Domain reservation entity and status enum
- Application DTOs and contracts
- Repository abstraction for future persistence
- Domain tests for entity-local lifecycle rules

Phase 2 adds backend persistence and create reservation behavior:

- MongoDB `EnergyReservations` collection document mapping
- Reservation repository implementation
- Reservation collection indexes
- `CreateReservationAsync` application workflow
- `POST /api/v1/reservations`
- Focused application, API, and repository tests

Phase 3 adds backend update, cancellation, querying, and ownership behavior:

- reservation update use case
- reservation cancellation use case
- owner and operational-role read behavior
- `GET /api/v1/reservations`
- `GET /api/v1/reservations/me`
- `GET /api/v1/reservations/{id}`
- `PUT /api/v1/reservations/{id}`
- `PATCH /api/v1/reservations/{id}/cancel`
- tests for notice-period boundaries, ownership, terminal states, and query filtering

Phase 4 adds backend approval, rejection, secure QR transaction verification, and completion:

- `ApproveReservationAsync`
- `RejectReservationAsync`
- secure QR token issuance for approved reservations
- server-side QR verification
- server-side energy transfer completion
- tests for review authorization, QR validity, invalid states, and duplicate completion

It does not add React or Android behavior.

## Required Business Rules For Later Phases

The API must enforce these rules authoritatively:

| Rule | Enforcement Layer |
| --- | --- |
| Reservation cannot be scheduled in the past. | Application service, implemented in Phase 2 |
| Reservation must be scheduled within 7 days. | Application service, implemented in Phase 2 |
| Reservation updates require at least 12 hours notice. | Application service, implemented in Phase 3 |
| Reservation cancellations require at least 12 hours notice. | Application service, implemented in Phase 3 |
| Inactive or unavailable station cannot be reserved. | Application service using station abstraction, implemented in Phase 2 |
| Inactive or unavailable booking slot cannot be reserved. | Application service using booking-slot abstraction, implemented in Phase 2 |
| Duplicate active booking for the same slot must be prevented. | Repository query, implemented in Phase 2 |
| Ownership checks are required for prosumer actions. | Application service using current-user/prosumer abstractions |
| Invalid status transitions must be rejected. | Domain entity and Application service |
| Completed transactions cannot be completed again. | Domain entity and Application service |

The notice-period interpretation is inclusive: update and cancellation are allowed when exactly 12 hours remain before the currently scheduled reservation, and rejected once less than 12 hours remain.

## MongoDB Persistence

The reservation repository stores documents in the configured `MongoDb:EnergyReservationsCollectionName` collection. The default collection name is `EnergyReservations`.

Indexes:

| Index | Fields | Purpose |
| --- | --- | --- |
| `ix_energy_reservations_slot_status` | `BookingSlotId`, `Status` | Active duplicate slot checks. |
| `ix_energy_reservations_prosumer_scheduled_at` | `ProsumerId`, `ScheduledAtUtc` | Prosumer reservation history ordered by schedule. |
| `ix_energy_reservations_station_status` | `StationId`, `Status` | Active reservation checks for station workflows. |

No QR transaction-token unique index is created yet because token generation is outside the current phase.

## Create Reservation Flow

`CreateReservationAsync` currently:

- validates required identifiers and schedule timestamp
- normalizes identifiers by trimming whitespace
- converts the requested schedule to UTC
- rejects past schedules
- rejects schedules later than exactly 7 days from the current UTC time
- verifies prosumer existence and active status through `IReservationProsumerReadService`
- verifies station existence and active status through `IReservationStationReadService`
- verifies slot existence, station ownership, active status, and availability through `IReservationBookingSlotReadService`
- prevents duplicate pending/approved reservations for the same booking slot
- creates a pending `EnergyReservation`
- persists through `IReservationRepository`
- returns `ReservationResponse` without QR token hash details

For non-Backoffice callers, reservation ownership is derived from the authenticated user id claim. Backoffice callers may create on behalf of a supplied prosumer id.

## Update Reservation Flow

`UpdateReservationAsync` currently:

- verifies the reservation exists
- verifies the caller owns the reservation or has an operational role
- rejects completed, rejected, and cancelled reservations
- enforces at least 12 hours notice against the current scheduled time
- validates the new scheduled time is not in the past
- validates the new scheduled time is within 7 days
- verifies the selected station exists and is active
- verifies the selected booking slot exists, belongs to the station, and is active/available
- prevents duplicate pending/approved reservations for the selected booking slot
- applies the station, slot, and schedule change through the domain entity
- preserves the historical record and updates the document

## Cancellation Flow

`CancelReservationAsync` currently:

- verifies the reservation exists
- verifies the caller owns the reservation or has an operational role
- rejects completed, rejected, and already-cancelled reservations
- enforces at least 12 hours notice against the current scheduled time
- calls the domain `Cancel` method
- persists the changed status and `CancelledAt` timestamp
- never physically deletes the reservation

## Query And Ownership

| Endpoint | Access |
| --- | --- |
| `GET /api/v1/reservations` | Backoffice and GridOperator only. Supports prosumer, station, booking slot, status, date range, search, and pagination filters. |
| `GET /api/v1/reservations/me` | Any authenticated caller. Forces `ProsumerId` to the current authenticated user id claim. |
| `GET /api/v1/reservations/{id}` | Owner, Backoffice, or GridOperator. |
| `PUT /api/v1/reservations/{id}` | Owner, Backoffice, or GridOperator. |
| `PATCH /api/v1/reservations/{id}/cancel` | Owner, Backoffice, or GridOperator. |
| `PATCH /api/v1/reservations/{id}/approve` | Backoffice or GridOperator. |
| `PATCH /api/v1/reservations/{id}/reject` | Backoffice or GridOperator. |
| `POST /api/v1/reservations/{id}/qr` | Reservation owner or Backoffice. |
| `POST /api/v1/reservations/verify-qr` | Backoffice or GridOperator. |
| `POST /api/v1/reservations/{id}/complete` | Backoffice or GridOperator. |

Request body ownership values are not authoritative. Ownership is checked from server-side claims.

## Approval And Rejection Flow

Approval and rejection are allowed only while a reservation is `Pending`.

`ApproveReservationAsync`:

- requires an operational role
- records the authenticated reviewer id as `ApprovedBy`
- records UTC `ApprovedAt`
- uses the domain `Approve` transition

`RejectReservationAsync`:

- requires an operational role
- validates that a rejection reason is present
- records the authenticated reviewer id as `RejectedBy`
- records UTC `RejectedAt`
- uses the domain `Reject` transition

Cancelled, rejected, and completed reservations cannot be approved or rejected.

## QR Transaction Design

The QR payload is not just a reservation id. The API issues a cryptographically random token for an approved reservation, stores only a SHA-256 hash of that token, and returns the raw token once in `ReservationQrResponse`.

The QR response contains:

- `ReservationId`
- `VerificationToken`
- `ExpiresAt`

The token lifetime is centralized as `ReservationTimeRules.QrVerificationTokenLifetimeMinutes`, currently 30 minutes. Issuing a new QR token replaces the stored hash and invalidates older QR payloads.

Raw QR tokens are not stored in MongoDB and no signing secrets are exposed to clients.

## Verification And Completion Flow

`VerifyReservationQrAsync`:

- requires Backoffice or GridOperator
- verifies the reservation exists
- requires the reservation to still be `Approved`
- rejects cancelled, completed, pending, and rejected reservations
- checks a stored token hash exists
- checks token expiry
- checks the supplied token against the stored hash
- records `QrVerifiedAt` when verification succeeds

`CompleteReservationAsync`:

- requires Backoffice or GridOperator
- revalidates the supplied QR token even if verification was already called
- requires the reservation to still be `Approved`
- rejects duplicate completion
- records UTC `CompletedAt`
- records authenticated operator id as `CompletedBy`
- uses the domain `Complete` transition

## Domain Statuses

| Status | Meaning |
| --- | --- |
| `Pending` | Reservation request has been created and awaits approval. |
| `Approved` | Reservation is accepted and can later be QR verified/completed. |
| `Rejected` | Reservation was rejected with reviewer metadata. |
| `Cancelled` | Reservation was cancelled before completion. |
| `Completed` | Energy transfer was finalized. |

## Domain Transition Rules

Allowed transitions:

- `Pending -> Approved`
- `Pending -> Rejected`
- `Pending -> Cancelled`
- `Pending -> Rescheduled`
- `Approved -> Cancelled`
- `Approved -> Completed`
- `Approved -> Rescheduled`
- `Approved -> QR token registered`
- `Approved -> QR verified`

Rejected transitions:

- `Completed -> Pending`
- `Completed -> Completed`
- `Completed -> Cancelled`
- `Cancelled -> Approved`
- `Rejected -> Completed`
- `Rejected -> Approved`

## Cross-Component Contracts Needed Before Live Integration

Reservation create logic now depends on focused contracts from other components:

- `IReservationProsumerReadService` to verify the prosumer exists and is active.
- `IReservationStationReadService` to verify the station exists and is active.
- `IReservationBookingSlotReadService` to verify the slot exists, belongs to the station, and is active/available.
- Current-user/ownership contract for prosumer-owned reservation actions.

Concrete adapters should be added by or coordinated with the owning components instead of duplicating their entities in Reservation Management.
