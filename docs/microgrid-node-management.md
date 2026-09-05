# Microgrid Node Management — Phase 1

## Scope and sources

The assignment specification (`../EAD_SE4040_Assignment_2026.pdf`) requires GPS coordinates, capacity, battery slots, operational schedules, and blocking node deactivation while active reservations exist. The Phase 1 request additionally requires explicit slot ownership and booking start/end times. No `AGENTS.md` or `members.txt` was present in the scanned repository/parent directory, so the existing backend conventions were followed.

This phase contains Domain behaviour, Application DTOs, validators, repository/service contracts, and tests only. It adds no controllers, repository implementations, service implementations, dependency-injection registrations, client changes, Maps features, or reservation workflows. The existing discovery and reservation-related domain methods are retained.

## Domain model

`SolarStation` retains its string identity, normalized uppercase code, name, address, `GeoCoordinates`, positive generation `CapacityKw`, weekly `OperatingWindow` schedule, `StationStatus`, and `DateTimeOffset` creation/update audit fields. There is no existing `BaseEntity`; auditing follows the existing entity conventions.

`TotalSlotCount` and `AvailableSlotCount` are derived from the owned slots. Counts cannot become negative or disagree with the slot list. Zero configured slots are allowed; such a station cannot accept bookings. There is no independently writable battery count. Collection views prevent adding/removing items outside validated domain operations. Failed detail/schedule changes leave the prior state intact.

`EnergyBookingSlot` retains its identity, unique per-station slot number, positive `BatteryCapacityKwh`, status, and audit fields. It now requires an immutable `StationId` and an absolute `StartTime`/`EndTime` interval stored in UTC. Station construction and slot addition reject foreign ownership, null entries, duplicate IDs, and duplicate slot numbers. A committed slot cannot be reconfigured or removed.

`IsAvailable` describes slot status; `IsAvailableAt(instant)` additionally checks the half-open interval `[StartTime, EndTime)`. `CanAcceptBookingsAt` combines station state, operating schedule, and interval-specific slot availability. These are local eligibility checks, not guarantees against concurrent reservations.

Existing schedule semantics are retained: nonempty, nonoverlapping same-day windows, with an exclusive closing time. Weekly checks use the weekday/time of the supplied offset-aware instant. A station timezone policy is not specified by the assignment; callers must supply the intended schedule-local offset until that policy is established.

## States

Existing enum names and numeric values are preserved:

- `StationStatus`: `Active = 1`, `Inactive = 2`.
- `SlotStatus`: `Available = 1`, `Reserved = 2`, `Occupied = 3`, `OutOfService = 4`.

Station activation/deactivation remain explicit domain operations. Deactivation rejects locally reserved/occupied slots; the future application service must also consult reservations. Slot availability is deactivated with `TakeOutOfService` and reactivated with `ReturnToService`. Existing reservation-related transitions remain `Available -> Reserved -> Occupied`, with reserved/occupied slots releasable to `Available`. Maintenance cannot interrupt a committed slot. Undefined enum values are rejected; requested transitions are checked against the loaded entity, not merely the request enum.

## Application contracts

Established station names are retained instead of creating duplicate aliases:

| Requested concept | Existing/refined type |
| --- | --- |
| Create/update station | `CreateSolarStationRequest`, `UpdateSolarStationRequest` |
| Station response/query | `SolarStationResponse`, `SolarStationQuery` |
| Station repository/service | `ISolarStationRepository`, `ISolarStationService` |
| Create booking slot | `CreateBookingSlotRequest` (renamed from `EnergyBookingSlotRequest`) |
| Update booking slot | `UpdateBookingSlotRequest` |
| Slot response/query | `EnergyBookingSlotResponse`, `BookingSlotQuery` |
| Slot repository/service | `IBookingSlotRepository`, `IBookingSlotService` |

Slot creation receives station identity from the owning station request or service argument, avoiding conflicting body and route IDs. Updates cannot reassign identity or ownership. `UpdateSlotStatusRequest` remains the separate status-change contract. Slot responses expose ownership, times, capacity, typed status, active/available flags, and audit values.

`ISolarStationService` inherits the focused slot service contract to retain its existing slot-management surface without duplicate method declarations. Queries and repositories use the existing `Common.Models.PagedResult<T>` and cancellation tokens; they expose no MongoDB types. Slot time filters select overlapping intervals, with either bound optional. Persistence implementation must maintain consistency between station slot membership and slot records; storage layout is not implemented in this phase.

## Cross-component boundary

Reuse the existing Reservation Management contracts rather than introducing competing lookup APIs:

- `IReservationStationReadService.GetByIdAsync`: null means missing; `Id` and `IsActive` convey station identity and eligibility.
- `IReservationBookingSlotReadService.GetByIdAsync`: null means missing; `Id`, `StationId`, `IsActive`, and `IsAvailable` allow reservation code to check existence, ownership, state, and availability.
- `IStationReservationLookup.CountActiveReservationsAsync`: enables the future station service to block deactivation when any active reservation exists, including reservations not reflected in a locally committed slot.

Adapters remain future work. These snapshots are advisory reads, not reservation locks. The Reservation component retains responsibility for duplicate booking prevention and its own 7-day/12-hour rules; this phase does not alter those rules or existing workflows.

## Validation and FAT service boundary

Validators reuse `IRequestValidator<T>`, `ValidationResult`, and the existing station validation helpers. Rules cover required code/name/address, existing length limits, finite latitude in `[-90, 90]`, finite longitude in `[-180, 180]`, positive capacities and slot numbers, valid ordered booking intervals, supported weekdays/statuses, schedule overlap, duplicate slot numbers, and positive paging. Existing maximum slot-list size is retained. Empty slot lists represent zero capacity slots; null lists/entries are invalid.

Domain validation independently protects entity invariants and current-state transitions. Request validators check payload shape and supported statuses; they cannot establish a transition's legality without loading current state. Application services must later invoke these validators, enforce authorization and database-dependent checks, and use the existing shared `ValidationException`, `NotFoundException`, and `ConflictException` conventions. No client-side enforcement is authoritative.

## Tests and verification

- `SolGrid.Domain.Tests/SolarStations/EnergyBookingSlotTests.cs`: identity/capacity/time invariants, UTC normalization, legal/illegal transitions, committed-slot protection, interval boundaries, and atomic edits.
- `SolGrid.Domain.Tests/SolarStations/SolarStationTests.cs`: station edits, activation/deactivation, counts, ownership, collection protection, schedules, GPS ranges, and invalid enum values.
- `SolGrid.Application.Tests/SolarStations/MicrogridContractTests.cs`: request/query validation and slot response mapping.

Projects target .NET 10. Run the backend build and Domain/Application tests with a .NET 10 SDK. No database is required for these tests.

## Files changed in this phase

Paths below are relative to `web-service/`. The two old `EnergyBookingSlotRequest` files are renamed to `CreateBookingSlotRequest` and its validator.

- `src/SolGrid.Application/SolarStations/Interfaces/ISolarStationService.cs`
- `src/SolGrid.Application/SolarStations/Requests/CreateSolarStationRequest.cs`
- `src/SolGrid.Application/SolarStations/Responses/EnergyBookingSlotResponse.cs`
- `src/SolGrid.Application/SolarStations/Responses/SolarStationResponseMapper.cs`
- `src/SolGrid.Application/SolarStations/Validation/SolarStationValidationRules.cs`
- `src/SolGrid.Domain/Entities/EnergyBookingSlot.cs`
- `src/SolGrid.Domain/Entities/SolarStation.cs`
- `src/SolGrid.Application/SolarStations/Interfaces/BookingSlotQuery.cs`
- `src/SolGrid.Application/SolarStations/Interfaces/IBookingSlotRepository.cs`
- `src/SolGrid.Application/SolarStations/Interfaces/IBookingSlotService.cs`
- `src/SolGrid.Application/SolarStations/Requests/CreateBookingSlotRequest.cs`
- `src/SolGrid.Application/SolarStations/Requests/UpdateBookingSlotRequest.cs`
- `src/SolGrid.Application/SolarStations/Validation/BookingSlotQueryValidator.cs`
- `src/SolGrid.Application/SolarStations/Validation/CreateBookingSlotRequestValidator.cs`
- `src/SolGrid.Application/SolarStations/Validation/SolarStationQueryValidator.cs`
- `src/SolGrid.Application/SolarStations/Validation/UpdateBookingSlotRequestValidator.cs`
- `src/SolGrid.Application/SolarStations/Validation/UpdateSlotStatusRequestValidator.cs`
- `tests/SolGrid.Application.Tests/SolarStations/MicrogridContractTests.cs`
- `tests/SolGrid.Domain.Tests/SolarStations/EnergyBookingSlotTests.cs`
- `tests/SolGrid.Domain.Tests/SolarStations/SolarStationTests.cs`

This document is also new. The pre-existing `web-app/.gitignore` edit is untouched.

## Verification results

- Used temporary .NET SDK 10.0.401 at `/tmp/solgrid-dotnet`; project target frameworks were unchanged.
- Domain suite: **69 passed**, zero failures or skips.
- Isolated M2 Application contract suite: **21 passed**, zero failures or skips. The temporary `/tmp/solgrid-m2-verification/SolGrid.M2.Contract.Tests.csproj` compiles the actual M2 source/tests, the shared validation/pagination types, and the Domain project.
- Isolated M2 build: **zero warnings, zero errors**.
- Formatting and formatting verification passed for the changed C# files; `git diff --check` passed. Header contributor/file names and method-entry comments were checked.
- Full API build and the normal Application suite are **blocked by 13 existing CS0246 errors**: Prosumer/Reservation interfaces and services reference `PagedResult<T>` without importing `SolGrid.Application.Common.Models`. A separate build of committed `HEAD` reproduced the same errors. Those unrelated files were not modified. No compiler warnings were reported before that build failure.
