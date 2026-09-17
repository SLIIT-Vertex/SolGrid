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

Android development uses the same root `.env`. Set:

```dotenv
MOBILE_API_BASE_URL=http://192.168.1.2:5080/
MOBILE_MAPS_API_KEY=your_android_maps_key
```

Use your computer's current LAN IP for a physical phone on the same network, or `http://10.0.2.2:5080/` for the Android emulator. The URL must end with `/`. For a local backend accessible from the phone, run `make run ASPNETCORE_ENVIRONMENT=Development ASPNETCORE_URLS=http://0.0.0.0:5080` after `make secrets-set`, or use `make docker-up`.

```text
make mobile-env
make mobile-build
make mobile-install
make mobile-test
make mobile-clean
```

`mobile-env` creates ignored `mobile-app/env.properties` containing only the Android API URL and Maps key. Build, install and unit-test commands refresh this file automatically. Android Studio reads it too; run `make mobile-env` after changing `.env` before building from the IDE. Backend passwords and JWT signing secrets are not copied into the Android configuration. `mobile-install` builds and installs the debug APK on connected devices using Gradle. The APK is at `mobile-app/app/build/outputs/apk/debug/app-debug.apk`.

Android commands require the Android SDK and JDK supported by the project, with SDK location configured in Android Studio or ignored `mobile-app/local.properties`. Settings are embedded at build time; rebuild and reinstall after changing them. Explicit Gradle `-PAPI_BASE_URL` / `-PMAPS_API_KEY` overrides remain available. To override the root setting through Make:

```text
make mobile-build MOBILE_API_BASE_URL=http://10.0.2.2:5080/
```

`env-init` preserves existing `.env` files. For older files, add the two `MOBILE_` variables manually. A missing URL defaults to the emulator address; a missing Maps key leaves the map unconfigured.
