# AGENTS.md

# SolGrid Development Instructions

## 1. Project Overview

SolGrid is a Smart Solar Microgrid Trading System developed for the SE4040 Enterprise Application Development assignment.

The system consists of three client/server components:

- ASP.NET Core C# Web API
- React web application
- Pure native Android application using Kotlin and SQLite

The backend follows the FAT Service pattern required by the assignment.

All authoritative business logic, validation, authorization, reservation rules, and database operations MUST be handled by the Web API.

The React and Android applications must act primarily as presentation/client layers and communicate with the backend exclusively through REST APIs.

---

# 2. Repository Structure

Maintain the following high-level structure:

```text
SolGrid/
├── web-service/
│   ├── SolGrid.slnx
│   ├── src/
│   │   ├── SolGrid.Api/
│   │   ├── SolGrid.Application/
│   │   ├── SolGrid.Domain/
│   │   └── SolGrid.Infrastructure/
│   └── tests/
│
├── web-app/
│
├── mobile-app/
│
├── docs/
│
├── members.txt
├── AGENTS.md
├── README.md
└── .gitignore
```

Do not restructure the repository without a clear technical reason.

---

# 3. Architecture Rules

## 3.1 Backend

The backend uses Clean Architecture internally while satisfying the assignment's FAT Service requirement.

Dependency direction:

```text
Api
 ↓
Application
 ↓
Domain

Infrastructure
 ↓
Application / Domain
```

Responsibilities:

### SolGrid.Domain

Contains:

- Entities
- Enums
- Core domain concepts
- Domain-specific value objects where appropriate

Domain MUST NOT depend on:

- ASP.NET Core
- MongoDB
- React
- Android
- Infrastructure implementations

### SolGrid.Application

Contains:

- Application services
- DTOs
- Repository interfaces
- Service interfaces
- Business rules
- Validation
- Application exceptions

All important business decisions belong here or in the Domain layer.

### SolGrid.Infrastructure

Contains:

- MongoDB integration
- Repository implementations
- Persistence configuration
- External service implementations
- Infrastructure-specific concerns

MongoDB-specific types must not leak unnecessarily into Application or Domain.

### SolGrid.Api

Contains:

- Controllers
- Authentication configuration
- Authorization configuration
- Middleware
- Dependency injection
- HTTP configuration
- API-specific concerns

Controllers MUST remain thin.

Controllers must not contain substantial business logic.

Preferred flow:

```text
HTTP Request
    ↓
Controller
    ↓
Application Service
    ↓
Repository Interface
    ↓
Infrastructure
    ↓
MongoDB
```

---

# 4. FAT Service Requirement

This requirement has priority over convenience.

All authoritative business logic MUST reside in the backend.

Never make React or Android the sole place where a business rule is enforced.

For example, the following rules MUST be validated by the API:

- Reservations must be scheduled within 7 days.
- Reservation updates require at least 12 hours' notice.
- Reservation cancellations require at least 12 hours' notice.
- Stations cannot be deactivated while active reservations exist.
- NIC uniqueness.
- Account activation and reactivation permissions.
- Role-based access.
- Reservation availability.
- QR transaction verification.
- Transaction completion.

Clients MAY duplicate simple validation for better UX, but the API must always perform the authoritative validation.

Clients MUST NOT access MongoDB directly.

---

# 5. Required Core Data Areas

The backend must support the assignment's required data areas:

```text
Users
SolarStationInfo
EnergyBookingSlots
EnergyReservations
```

References between these collections must remain consistent.

Do not introduce duplicate sources of truth.

Use enums or strongly typed values for statuses and roles instead of scattered magic strings.

Examples:

```text
UserRole
AccountStatus
StationStatus
ReservationStatus
```

---

# 6. Coding Standards

All generated and modified code must follow professional enterprise-level coding practices.

Priorities:

1. Correctness
2. Readability
3. Maintainability
4. Security
5. Testability
6. Consistency
7. Performance where relevant

Do not sacrifice readability for clever code.

---

# 7. SOLID Principles

Apply SOLID principles pragmatically.

## Single Responsibility

A class should have one clear responsibility.

Avoid services that become unrelated collections of methods.

## Open/Closed

Prefer designs that can be extended without repeatedly rewriting stable code.

Do not over-engineer speculative extension points.

## Liskov Substitution

Implementations must respect their interfaces and expected behaviour.

## Interface Segregation

Prefer focused interfaces.

Avoid large interfaces containing unrelated operations.

## Dependency Inversion

Application logic should depend on abstractions rather than MongoDB or other infrastructure implementations.

---

# 8. Avoid Code Smells

Actively identify and avoid:

- God classes
- Fat controllers
- Long methods
- Deep nesting
- Duplicate code
- Magic numbers
- Magic strings
- Boolean parameter abuse
- Excessive static state
- Tight coupling
- Circular dependencies
- Dead code
- Unused imports
- Unused variables
- Copy-pasted validation
- Business logic inside controllers
- Business logic inside React components
- Business logic inside Android Activities/Fragments
- Direct MongoDB access outside Infrastructure
- Generic catch blocks that hide failures
- Empty catch blocks
- Hard-coded credentials
- Hard-coded API URLs
- Hard-coded user IDs
- Hard-coded reservation/station data
- Premature abstraction
- Premature optimization

When a method becomes difficult to understand, refactor it into smaller meaningful operations.

Do not create abstractions merely to increase the number of layers.

---

# 9. Naming Standards

Use meaningful names.

Good:

```text
ReservationService
CreateReservationAsync
GetActiveReservationsAsync
CanDeactivateStationAsync
ReservationStatus
minimumCancellationNotice
```

Bad:

```text
Manager
Helper
DoStuff
ProcessData
x
temp
obj
data2
```

Use established language conventions.

C#:

```text
PascalCase → classes, methods, properties
camelCase  → local variables and parameters
IName      → interfaces
```

TypeScript:

```text
PascalCase → components/types
camelCase  → functions/variables
```

Kotlin:

Follow standard Kotlin and Android naming conventions.

---

# 10. Async Programming

For C# I/O operations, use async/await consistently.

Prefer:

```csharp
Task<User?>
Task<IReadOnlyList<User>>
```

Avoid blocking asynchronous code using:

```csharp
.Result
.Wait()
```

Use the `Async` suffix for asynchronous methods.

---

# 11. DTO Rules

Do not expose persistence entities directly through API endpoints unless there is a strong reason.

Use request and response DTOs.

Examples:

```text
LoginRequest
LoginResponse

RegisterProsumerRequest
UpdateProsumerRequest
ProsumerResponse

CreateStationRequest
UpdateStationRequest
StationResponse

CreateReservationRequest
UpdateReservationRequest
ReservationResponse
```

Never allow clients to update protected fields merely because those fields exist on an entity.

---

# 12. Validation

Validate input at API boundaries and enforce business invariants in the Application/Domain layer.

Validate:

- Required fields
- Email formats
- NIC formats where applicable
- Coordinates
- Positive capacity values
- Dates
- Time ranges
- Reservation constraints
- Entity existence
- Status transitions
- Role permissions

Never trust client-side validation.

Return useful validation errors.

---

# 13. Error Handling

Use centralized/global exception handling.

Do not place repetitive try/catch blocks in every controller.

Use meaningful exception categories where appropriate:

```text
ValidationException
NotFoundException
ConflictException
BusinessRuleException
UnauthorizedAccessException
```

API error responses should be consistent and must not expose stack traces or sensitive internal information.

---

# 14. Security

Never:

- Store plaintext passwords
- Commit secrets
- Commit database credentials
- Commit JWT signing keys
- Trust role information supplied by clients
- Trust reservation IDs without server verification
- Trust QR contents without server verification

Use:

- Secure password hashing
- JWT authentication
- Role-based authorization
- Server-side ownership checks
- Environment/configuration-based secrets
- Input validation

Authorization must happen on the server.

Hiding a button in React or Android is NOT authorization.

---

# 15. MongoDB Practices

MongoDB access belongs in Infrastructure.

Use repositories consistently.

Create appropriate indexes for frequently queried and unique fields.

NIC uniqueness must be protected at database/application level as appropriate.

Avoid loading entire collections when a filtered query can be performed by MongoDB.

Use UTC for persisted timestamps unless there is a documented reason otherwise.

Keep creation/update timestamps consistent.

---

# 16. React Standards

The React application is primarily a UI layer.

Use:

- TypeScript
- Reusable components
- Central API client
- Clear page/component separation
- Loading states
- Error states
- Empty states
- Responsive UI
- Consistent styling

Do not:

- Directly access MongoDB
- Reimplement authoritative business rules
- Hard-code API responses
- Put every API call directly inside page components
- Create huge components

Suggested organization:

```text
src/
├── api/
├── components/
├── layouts/
├── pages/
├── types/
├── hooks/
└── utils/
```

---

# 17. Android Standards

The mobile application MUST remain pure native Android.

Use Kotlin.

Do NOT introduce:

- React Native
- Flutter
- Capacitor
- Ionic
- Expo
- Other cross-platform frameworks

The application must use SQLite for required local persistence.

Keep UI, networking, persistence, and domain representation separated.

Suggested organization:

```text
data/
├── local/
├── remote/
└── repository/

model/

ui/
├── auth/
├── dashboard/
├── profile/
├── booking/
├── map/
└── operator/

utils/
```

The mobile app must obtain authoritative business data from the Web API.

---

# 18. Required C# Comments

The assignment explicitly requires a comment header block on each `.cs` file.

Every new `.cs` file must contain a header similar to:

```csharp
/*
 * Project: SolGrid
 * Module: SE4040 Enterprise Application Development
 * File: ReservationService.cs
 * Description: Handles energy reservation business operations.
 * Contributor: <actual contributor>
 */
```

The assignment also requires an inline comment at the beginning of each method.

Example:

```csharp
public async Task<ReservationResponse> CreateReservationAsync(
    CreateReservationRequest request)
{
    // Validate and create an energy reservation according to the system business rules.

    ...
}
```

Keep comments useful and concise.

Do not write misleading comments.

---

# 19. Testing Standards

Important business rules should have automated tests.

Priority tests:

- Duplicate NIC rejection
- Login behaviour
- Role authorization
- Reservation within 7 days
- Reservation beyond 7 days
- Update with more than 12 hours notice
- Update inside 12 hours
- Cancellation with more than 12 hours notice
- Cancellation inside 12 hours
- Station deactivation without active reservations
- Station deactivation with active reservations
- Invalid reservation status transitions
- Invalid QR
- Valid QR
- Duplicate transaction completion

Tests should describe behaviour rather than implementation details.

---

# 20. Definition of Done

A feature is NOT complete merely because it compiles.

Before considering a feature complete:

- Code builds successfully.
- Required behaviour works.
- Business rules are enforced server-side.
- Input validation exists.
- Authorization is correct.
- Errors are handled.
- Relevant tests pass.
- No obvious code smells remain.
- No credentials are committed.
- Required comments are present.
- Existing functionality is not broken.
- Documentation is updated when necessary.

---

# 21. Git Workflow

Use feature branches.

Never develop substantial features directly on `main`.

Preferred workflow:

```text
main
  ↑
feature branch
  ↑
small logical commits
```

Examples:

```text
feature/api-authentication
feature/api-prosumer-management
feature/api-station-management
feature/api-reservations

feature/web-authentication
feature/web-user-management
feature/web-stations
feature/web-reservations

feature/android-authentication
feature/android-prosumer-profile
feature/android-bookings
feature/android-map
feature/android-qr
```

When a feature is complete and verified, merge it into `main` through the team's agreed review process.

Do not leave long-running branches unnecessarily far behind `main`.

---

# 22. Commit Standards

Commit frequently when a meaningful unit of work is complete.

Do NOT create a commit after every trivial line change.

A commit should represent one understandable development step.

Use Conventional Commit style:

```text
feat(api): add MongoDB configuration

feat(api): implement prosumer registration

feat(api): add JWT authentication

feat(api): enforce seven-day reservation rule

feat(web): add backoffice login page

feat(web): implement station management table

feat(android): add prosumer registration screen

feat(android): persist session data in SQLite

feat(android): display stations on Google Maps

feat(android): implement transaction QR scanner

fix(api): prevent station deactivation with active bookings

refactor(api): extract reservation validation

test(api): cover reservation cancellation rules

docs: document IIS deployment

chore: update project configuration
```

Avoid meaningless messages:

```text
update
changes
fix
final
final2
work
done
test
new
```

---

# 23. Commit Integrity

Git history must represent real development activity.

Never:

- Fabricate commits.
- Backdate commits to create an artificial development history.
- Impersonate another member.
- Change Git authorship to make work appear to belong to someone else.
- Create fake contributions for assessment purposes.
- Attribute generated work to a member who did not perform or review that work.

When making a commit, use the Git identity of the member who actually performed or took responsibility for that work.

`members.txt` may be used to identify the team's GitHub accounts and contact information, but it must NOT be used to impersonate contributors.

Do not automatically modify global Git identity.

Do not use environment variables such as `GIT_AUTHOR_DATE`, `GIT_COMMITTER_DATE`, `GIT_AUTHOR_NAME`, or `GIT_AUTHOR_EMAIL` to manufacture history.

Preserve existing genuine commits beginning from the project's actual development start.

---

# 24. Team Responsibilities

There are four team members.

Read `members.txt` when contributor information is needed.

Suggested high-level ownership:

## Member 1

Primary responsibility:

```text
Web Service / Backend
```

Areas:

- Architecture
- ASP.NET Core
- MongoDB
- Authentication
- Business rules
- REST APIs
- IIS deployment
- Backend tests

## Member 2

Primary responsibility:

```text
Web Application
```

Areas:

- Backoffice UI
- Grid Operator web UI
- User management
- Prosumer management UI
- Station management UI
- Reservation management UI
- Responsive styling

## Member 3

Primary responsibility:

```text
Android Prosumer Experience
```

Areas:

- Prosumer registration
- Authentication UI
- Profile management
- Reservation creation
- Reservation modification/cancellation
- Booking history
- Dashboard
- SQLite integration

## Member 4

Primary responsibility:

```text
Android Operator and Device Features
```

Areas:

- Grid Operator mobile mode
- Google Maps
- Nearby stations
- QR display/scanning integration
- Server verification flow
- Energy transfer completion
- Supporting Android integration

Ownership does NOT prevent collaboration.

Cross-component work should be attributed to whoever actually performed it.

---

# 25. Integration Strategy

Develop components independently where practical but integrate continuously.

Recommended order:

```text
Backend API contract
       ↓
Backend implementation
       ↓
Swagger verification
       ↓
 ┌───────────────┐
 │               │
Web           Android
 │               │
 └───────┬───────┘
         ↓
Integration testing
         ↓
IIS deployment
         ↓
Final end-to-end testing
```

Frontend developers may use agreed API contracts or temporary mocks while backend endpoints are under development.

Mocks must be removed or clearly isolated before final submission.

---

# 26. Before Editing Code

Before implementing a task:

1. Read the relevant existing code.
2. Understand the architecture.
3. Check whether equivalent functionality already exists.
4. Identify the correct layer.
5. Determine affected tests.
6. Make the smallest coherent change.

Do not rewrite unrelated code.

Do not replace working implementations simply because another style is preferred.

---

# 27. After Editing Code

After a meaningful change:

1. Format the code.
2. Build the affected project.
3. Run relevant tests.
4. Check for warnings/errors.
5. Review the diff.
6. Remove debug code.
7. Remove unused code.
8. Check for exposed secrets.
9. Verify required comments.
10. Commit the logical unit of work using a meaningful message.

Do not commit broken code intentionally.

---

# 28. Codex Behaviour

When operating autonomously:

- Inspect before modifying.
- Prefer incremental changes.
- Do not invent requirements.
- Do not silently weaken business rules.
- Do not bypass tests to make builds pass.
- Do not disable compiler/linter warnings merely to hide problems.
- Do not remove security controls for convenience.
- Do not introduce unnecessary dependencies.
- Explain significant architectural decisions in documentation.
- Preserve backward compatibility where practical.
- Stop and report genuine ambiguity when a decision could substantially alter assignment requirements.

When several implementations are possible, prefer the simplest design that satisfies:

- Assignment requirements
- Clean Architecture
- FAT Service architecture
- Maintainability
- Security
- Testability
- Viva explainability

---

# 29. Assignment Priority

When there is a conflict between:

1. Personal coding preference
2. Framework convention
3. Existing project convention
4. Explicit assignment requirement

the explicit assignment requirement takes priority.

The goal is a complete, maintainable system that satisfies the marking scheme and can be clearly explained by the team during the viva.