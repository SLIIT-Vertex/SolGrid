# Development commands

Use Node.js 22.12+ and npm for the task runner and web app, the .NET SDK required by the API project for backend tasks, and Docker with Compose for container tasks. No additional Node packages are needed for the runner.

Run from the repository root in Windows PowerShell or Command Prompt, macOS, or Linux:

```text
node scripts/tasks.mjs help
node scripts/tasks.mjs env-init
node scripts/tasks.mjs secrets-set
node scripts/tasks.mjs run
```

For local API development, set `ASPNETCORE_ENVIRONMENT=Development` in `.env` before `secrets-set` and `run`. The example file defaults to `Production` for Docker; .NET user-secrets are loaded in Development.

To run the full stack in Docker:

```text
node scripts/tasks.mjs docker-up
node scripts/tasks.mjs docker-logs
node scripts/tasks.mjs docker-down
```

Backend validation:

```text
node scripts/tasks.mjs restore
node scripts/tasks.mjs verify
```

Web development:

```text
node scripts/tasks.mjs web-install
node scripts/tasks.mjs web-dev
node scripts/tasks.mjs web-build
```

All previous Make targets are available with the same names. GNU Make is optional: `make build` delegates to `node scripts/tasks.mjs build`. Bash, OpenSSL, sed, and curl are no longer needed by the runner.

Configuration precedence is command-line `KEY=value` overrides, then environment variables, then `.env`, then built-in defaults. Use quoted `.env` values for secrets containing `#` or surrounding whitespace. Values are parsed as dotenv data, without shell execution or variable interpolation. For example:

```text
node scripts/tasks.mjs run ASPNETCORE_ENVIRONMENT=Development
node scripts/tasks.mjs health BASE_URL=http://localhost:5080
```

With Make, use environment variables or `.env` to configure tasks; Make command-line variables are exported to the runner by GNU Make. `env-init` generates secrets only when `.env` is missing; both `env-init` and `env-example` preserve existing files. New `.env` files use owner-only permissions on Unix; Windows access follows filesystem permissions.

Mongo repository integration tests require a running test MongoDB and an explicit connection string:

```text
node scripts/tasks.mjs docker-test-mongo-up
node scripts/tasks.mjs test SOLGRID_MONGO_TEST_CONNECTION_STRING=mongodb://localhost:27018
node scripts/tasks.mjs docker-test-mongo-down
```

Run the task runner tests with `node --test scripts/tasks.test.mjs`.
