# Completed partial features

Backoffice can create pending prosumers with `POST /api/v1/prosumers` and edit profile fields with `PUT /api/v1/prosumers/{nic}`. NIC, password and account status are not part of profile updates. Activation remains a separate action. Both routes require Backoffice authorization in the controller and service.

The web reservation page exposes create and edit forms, using station/slot reference data and the existing reservation API. Times entered in local browser time are converted to ISO UTC. Seven-day scheduling and twelve-hour change notice are enforced centrally.

Grid Operators can activate/deactivate slots on the web node page and native mobile node details. Backoffice retains specification editing. API availability checks reject changes to committed slots.

Mobile reservation lists load all pages. History includes rejected/cancelled/completed and elapsed bookings. The owner dashboard uses server totals at `GET /api/v1/reservations/me/dashboard/summary`, scoped to the authenticated prosumer. Approved-future counts include bookings scheduled at or after server now. Reference metadata is cached in SQLite independently of authoritative reservation operations. SQLite schema version 2 preserves version 1 sessions during migration. Logout clears session and cached reference data.

Android uses `BuildConfig.API_BASE_URL`. Build for a physical device with:

```sh
cd mobile-app
./gradlew :app:assembleDebug -PAPI_BASE_URL=http://192.168.1.2:5080/
```

Use `http://10.0.2.2:5080/` for the emulator. For IIS use the IIS site's reachable URL. URLs must end with `/`.

Real QR image generation/camera scanning are now implemented; see qr-transactions.md. IIS hosting and submission report/video/screenshots remain separate unfinished deliverables. This change does not establish those as complete.

## Validation

Backend build, 70 domain tests, 138 application tests, and 40 API tests pass. All 27 repository tests also pass against the local running MongoDB, using isolated temporary databases that are removed by test teardown. The web production build, lint, and 18 tests pass. Android debug builds and its existing unit test pass; the added SQLite reference cache instrumentation test passes on the emulator. The phone debug APK was installed and its existing signed-in profile and live dashboard loaded through the LAN API.

Google Maps opens on the phone, but its tiles remain blank. No active grid stations currently exist in the local application database. Tile rendering still requires checking Google Cloud configuration and device Google Play services; map rendering is not recorded as passing. Follow https://developers.google.com/maps/documentation/android-sdk/get-api-key with package `com.solgrid.mobile` and debug SHA-1 `06:F3:54:D5:E6:72:FE:B5:6E:73:FC:C5:9E:03:32:F9:86:F2:C2:7C`.
