# Prosumer account management

Prosumer accounts are managed through the React Backoffice web app and native Android app. The C# API owns validation, authorization, lifecycle rules, history and MongoDB persistence. NIC remains the immutable account identifier.

## Account lifecycle

```text
Register -> Pending -> Backoffice activation -> Active
Active -> Prosumer requests deactivation -> DeactivationRequested
DeactivationRequested -> Backoffice review/deactivation -> Deactivated
Deactivated -> Backoffice reactivation -> Active
```

Backoffice can also deactivate a pending or active account directly.

- `Pending`: cannot sign in until Backoffice activates the account.
- `Active`: can use the account and submit one deactivation request.
- `DeactivationRequested`: remains usable, including login, profile editing and bookings, until Backoffice completes its review. A second request is rejected.
- `Deactivated`: cannot sign in. Existing JWTs are rejected on subsequent authenticated API requests, including reservation routes. Android clears the session and returns to login. Only Backoffice can reactivate the account.

Deactivation does not delete profiles or automatically cancel bookings. Existing reservation rules still apply. There is no new reservation-based deactivation restriction.

## Web and mobile

On the web, open **Prosumers** and select **Deactivation requested** to review requests. Use **History** to read the request reason before deactivating. Activation, deactivation and reactivation require a reason of 1–500 characters. Status cards and lists refresh after actions.

On Android, **Profile → Request Account Deactivation** collects the reason and confirmation. The profile then shows that review is pending and remains usable. **Profile → Account History** shows paginated events. **Refresh profile** retrieves the latest account details.

## History and conflict protection

The API records registration, profile updates, activation, deactivation requests, deactivation and reactivation. Events contain the action, resulting status, UTC time, account version, trusted actor identity/role and any lifecycle reason. Profile edits do not store old/new personal details or passwords. Owners see only their own history; internal staff identifiers are shown only to Backoffice. Grid Operators cannot access standalone account history.

Each profile response includes `version`. Every profile update and lifecycle command must send the version that the user reviewed as `expectedVersion`. Missing/negative versions return `400`; stale versions return `409`. Clients show the error and require the user to close/reload and review the latest account before retrying. They do not silently retry or overwrite newer data.

MongoDB compares NIC and the previous version in a single atomic replacement. Profile/status, the incremented version and account history are saved together. A losing concurrent write saves neither its profile changes nor its audit event. NIC and email uniqueness remain enforced by MongoDB indexes.

Existing documents without `Version` or `Activity` are read as version zero with empty history and upgraded on their next successful write. Earlier events are not fabricated. Embedded account history is suitable for this assignment; very large production histories would need separate archival/storage planning.

## API

All routes below start with `/api/v1/prosumers`.

| Route | Access | Purpose |
| --- | --- | --- |
| `POST /register` | Anonymous | Create a pending account. |
| `POST /login` | Anonymous | Verify email/password and account eligibility. |
| `GET /me`, `PUT /me` | Eligible prosumer | Read/update own profile. |
| `PATCH /me/request-deactivation` | Active prosumer | Submit a reasoned deactivation request. |
| `GET /me/activity` | Eligible prosumer | Read own history. |
| `GET /`, `GET /pending`, `GET /{nic}` | Backoffice | Search/filter or read profiles. |
| `POST /`, `PUT /{nic}` | Backoffice | Create/update a profile. |
| `PATCH /{nic}/activate`, `/deactivate`, `/reactivate` | Backoffice | Perform a reasoned lifecycle action. |
| `GET /{nic}/activity` | Backoffice | Review account history and actors. |

Lifecycle request example (use the latest profile version):

```json
{ "expectedVersion": 3, "reason": "Prosumer request reviewed and approved." }
```

Profile updates send `expectedVersion` alongside first name, last name, email and optional phone number. NIC, account status and passwords cannot be changed through profile updates. History accepts `pageNumber` and `pageSize` (1–100), and returns newest events first.

Errors use `application/problem+json`: `400` validation, `401` missing/invalid/revoked token, `403` insufficient permission or inactive login, `404` missing account, and `409` stale version, duplicate identity or illegal lifecycle transition. Deploy the API and both clients together because older clients do not send the required versions/reasons.

## Verification

Run `make verify` with .NET 10. For actual persistence tests, start `docker compose -f docker-compose.test.yml up -d` and run `make test SOLGRID_MONGO_TEST_CONNECTION_STRING=mongodb://localhost:27018`.

Regression coverage includes request-pending account eligibility, history ordering/pagination and permissions, required reasons/versions, stale profile/status writes, rejected tokens after deactivation, MongoDB atomic updates and legacy documents. Also run `npm run build`, `npm test`, `npm run lint` in `web-app`, and Android `:app:assembleDebug :app:testDebugUnitTest`.

Password reset and email verification remain outside this component's implemented scope.
