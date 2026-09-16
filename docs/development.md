# Development commands

Install GNU Make, Node.js 22.12+ and npm, the .NET SDK required by the API project for backend tasks, and Docker with Compose for container tasks. Ensure these tools are available on PATH.

Run from the repository root in Windows PowerShell or Command Prompt, macOS, or Linux:

```text
make help
make env-init
make secrets-set
make run
```

For local API development, set `ASPNETCORE_ENVIRONMENT=Development` in `.env` before `secrets-set` and `run`. The example file defaults to `Production` for Docker; .NET user-secrets are loaded in Development.

To run the full stack in Docker:

```text
make docker-up
make docker-logs
make docker-down
```

Backend validation:

```text
make restore
make verify
```

Web development:

```text
make web-install
make web-dev
make web-build
```

Use `make help` to list all available tasks. Bash, OpenSSL, sed, and curl are not required for these commands.

Configuration precedence is command-line `KEY=value` overrides, then environment variables, then `.env`, then built-in defaults. Use quoted `.env` values for secrets containing `#` or surrounding whitespace. Values are parsed as dotenv data, without shell execution or variable interpolation. For example:

```text
make run ASPNETCORE_ENVIRONMENT=Development
make health BASE_URL=http://localhost:5080
```

Use Make command-line variables, environment variables, or `.env` to configure tasks. `env-init` generates secrets only when `.env` is missing; both `env-init` and `env-example` preserve existing files. New `.env` files use owner-only permissions on Unix; Windows access follows filesystem permissions.

Mongo repository integration tests require a running test MongoDB and an explicit connection string:

```text
make docker-test-mongo-up
make test SOLGRID_MONGO_TEST_CONNECTION_STRING=mongodb://localhost:27018
make docker-test-mongo-down
```
