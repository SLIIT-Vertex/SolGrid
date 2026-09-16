# Microgrid Node Management (Android)

## Scope
Prosumers discover nearby stations and inspect read-only availability. Grid Operators browse stations and monitor node-scoped reservations. Backoffice remains responsible for station, schedule, and slot administration.

## API
- `GET /api/v1/stations` is GridOperator/Backoffice only.
- `GET /api/v1/stations/nearby`, `GET /api/v1/stations/{id}`, and slot reads use the authenticated session.
- `GET /api/v1/reservations?stationId={id}` powers operator monitoring.

Station power is shown in kW; booking-slot battery capacity is shown in kWh. Weekly schedule values are clock hours with no returned timezone.

## Run
The default API base URL is `http://192.168.1.2:5080/` for the current physical-device setup. Override it with `-PAPI_BASE_URL=http://10.0.2.2:5080/` for the emulator, or with your reachable server URL. Rebuild after changing the URL. Copy `secrets.properties.example` to ignored `secrets.properties` and add a Google Maps Android key restricted to `com.solgrid.mobile` and its signing certificate.

## Known dependency
Grid Operators can activate/deactivate battery slots from node details. Slot specifications and node schedules remain Backoffice-only. The server rejects availability changes for committed slots. Node and slot reference responses are cached in SQLite; booking operations always require the live API.
