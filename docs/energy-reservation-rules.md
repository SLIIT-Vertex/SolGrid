# Energy Slot Reservation Management Backend

## Phase 1 Scope

This phase adds the Energy Slot Reservation Management backend foundation only:

- Domain reservation entity and status enum
- Application DTOs and contracts
- Repository abstraction for future persistence
- Domain tests for entity-local lifecycle rules

It does not add controllers, MongoDB repository implementation, QR generation, React, Android, or full create/update/cancel use-case logic.

## Required Business Rules For Later Phases

The API must enforce these rules authoritatively:

| Rule | Enforcement Layer |
| --- | --- |
| Reservation cannot be scheduled in the past. | Application service |
| Reservation must be scheduled within 7 days. | Application service |
| Reservation updates require at least 12 hours notice. | Application service |
| Reservation cancellations require at least 12 hours notice. | Application service |
| Inactive or unavailable station cannot be reserved. | Application service using station abstraction |
| Inactive or unavailable booking slot cannot be reserved. | Application service using booking-slot abstraction |
| Duplicate active booking for the same slot must be prevented. | Repository query plus persistence uniqueness/transaction strategy |
| Ownership checks are required for prosumer actions. | Application service using current-user/prosumer abstractions |
| Invalid status transitions must be rejected. | Domain entity and Application service |
| Completed transactions cannot be completed again. | Domain entity and Application service |

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

## Cross-Component Contracts Needed Before Phase 2

Reservation use-case implementation will need focused contracts from other components:

- Prosumer lookup/status contract to verify the prosumer exists and is active.
- Station lookup/status contract to verify the station exists and is active/available.
- Booking slot lookup/status contract to verify the slot exists and is available.
- Current-user/ownership contract for prosumer-owned reservation actions.

These contracts should be added by or coordinated with the owning components instead of duplicating their entities in Reservation Management.
