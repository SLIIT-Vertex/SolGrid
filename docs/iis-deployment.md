# IIS Deployment

The assignment requires the C# Web API to be hosted on Windows IIS with MongoDB behind it, reachable by both clients. The API targets `net10.0`; no code changes are needed.

## Prerequisites (once per machine)

1. Enable IIS (Windows Features -> Internet Information Services + Management Console).
2. Install the **.NET 10 Hosting Bundle** (install after IIS), then run `iisreset`.
3. Have MongoDB reachable (local install or Atlas). Seed data: `mongosh "<connection-string>" --file scripts/seed-data.js`.

## Deploy

From an elevated PowerShell in the repo root:

```powershell
.\scripts\deploy-iis.ps1 -MongoConnectionString "mongodb://localhost:27017" `
    -JwtSigningKey "<long-random-secret>" -AllowedOrigin "http://<web-app-origin>"
```

The script publishes to `C:\inetpub\solgrid-api`, creates a "No Managed Code" app pool and site (default port 8081), sets the app-pool environment variables (`MongoDb__ConnectionString`, `Jwt__SigningKey`, `Cors__AllowedOrigins__0`, `ASPNETCORE_ENVIRONMENT`) and opens the firewall port. Re-running it republishes and updates the variables.

Manual equivalent: `dotnet publish web-service/src/SolGrid.Api -c Release -o C:\inetpub\solgrid-api`, then create the app pool/site in IIS Manager and set the same variables on the app pool.

## Verify

```powershell
curl http://localhost:8081/health
curl http://localhost:8081/openapi/v1.json
```

## Point the clients at it

- Android: set `MOBILE_API_BASE_URL=http://<host-LAN-IP>:8081/` (trailing slash) in `.env`, run `make mobile-env`, rebuild. A physical phone cannot use `10.0.2.2`. The manifest already allows cleartext HTTP.
- Web: set `VITE_API_BASE_URL=http://<host>:8081` in `web-app/.env`, run `make web-build`, and add the web origin to `Cors__AllowedOrigins`.

## Troubleshooting

- 500.19 / 502.5: Hosting Bundle missing or wrong version; app pool must be "No Managed Code".
- 500.30: app crashed at startup - usually empty `Jwt__SigningKey` or unreachable MongoDB. Set `stdoutLogEnabled="true"` in the published `web.config` and read `logs\`.
