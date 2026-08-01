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

It does not add update, cancel, approval, QR generation, React, or Android behavior.

## Required Business Rules For Later Phases

The API must enforce these rules authoritatively:

| Rule | Enforcement Layer |
| --- | --- |
| Reservation cannot be scheduled in the past. | Application service, implemented in Phase 2 |
| Reservation must be scheduled within 7 days. | Application service, implemented in Phase 2 |
| Reservation updates require at least 12 hours notice. | Application service |
| Reservation cancellations require at least 12 hours notice. | Application service |
| Inactive or unavailable station cannot be reserved. | Application service using station abstraction, implemented in Phase 2 |
| Inactive or unavailable booking slot cannot be reserved. | Application service using booking-slot abstraction, implemented in Phase 2 |
| Duplicate active booking for the same slot must be prevented. | Repository query, implemented in Phase 2 |
| Ownership checks are required for prosumer actions. | Application service using current-user/prosumer abstractions |
| Invalid status transitions must be rejected. | Domain entity and Application service |
| Completed transactions cannot be completed again. | Domain entity and Application service |

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
