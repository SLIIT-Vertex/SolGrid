# Microgrid Node Management (Android)

## Scope
Prosumers discover nearby stations and inspect read-only availability. Grid Operators browse stations and monitor node-scoped reservations. Backoffice remains responsible for station, schedule, and slot administration.

## API
- `GET /api/v1/stations` is GridOperator/Backoffice only.
- `GET /api/v1/stations/nearby`, `GET /api/v1/stations/{id}`, and slot reads use the authenticated session.
- `GET /api/v1/reservations?stationId={id}` powers operator monitoring.

Station power is shown in kW; booking-slot battery capacity is shown in kWh. Weekly schedule values are clock hours with no returned timezone.

## Run
For the Android emulator the API base URL is `http://10.0.2.2:5080/`. A physical device must use a reachable LAN URL. Copy `secrets.properties.example` to ignored `secrets.properties` and add a Google Maps Android key restricted to `com.solgrid.mobile` and its signing certificate.

## Known dependency
GridOperator slot availability updates are not available: every slot write endpoint is Backoffice-only. The mobile app intentionally remains read-only until the backend supplies an authorized GridOperator route.
