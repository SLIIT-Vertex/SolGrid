# SolGrid development commands. Configuration is loaded from the ignored root .env file.
# Keep configuration values on single KEY=value lines. For an one-off override, use:
# make BACKEND_PORT=5081 docker-up

SHELL := /bin/bash
.DEFAULT_GOAL := help

-include .env

API_PROJECT := web-service/src/SolGrid.Api/SolGrid.Api.csproj
DOMAIN_TESTS := web-service/tests/SolGrid.Domain.Tests/SolGrid.Domain.Tests.csproj
APPLICATION_TESTS := web-service/tests/SolGrid.Application.Tests/SolGrid.Application.Tests.csproj
API_TESTS := web-service/tests/SolGrid.Api.Tests/SolGrid.Api.Tests.csproj
INFRA_TESTS := web-service/tests/SolGrid.Infrastructure.Tests/SolGrid.Infrastructure.Tests.csproj
DOTNET_FLAGS := --no-restore -v minimal -m:1 -nr:false -p:UseSharedCompilation=false

ASPNETCORE_ENVIRONMENT ?= Development
ASPNETCORE_URLS ?= http://localhost:5080
BASE_URL ?= http://localhost:5080
MONGO_ROOT_USERNAME ?= solgrid_admin
MONGO_DATABASE ?= SolGrid
MONGO_PORT ?= 27017
MONGO_TEST_PORT ?= 27018
MONGO_USERS_COLLECTION ?= Users
MONGO_ENERGY_RESERVATIONS_COLLECTION ?= EnergyReservations
MONGO_INITIALIZE_ON_STARTUP ?= true
JWT_ISSUER ?= SolGrid
JWT_AUDIENCE ?= SolGrid.Web
JWT_EXPIRES_MINUTES ?= 60
WEB_APP_ORIGIN ?= http://localhost:5173
WEB_APP_DOCKER_ORIGIN ?= http://localhost:8080
VITE_API_BASE_URL ?= http://localhost:5080
VITE_API_PROXY_TARGET ?= http://localhost:5080
MOBILE_API_BASE_URL ?= http://10.0.2.2:5080/
# Android reads this at build time. Reuse the web Maps key when a separate Android key is not set.
MOBILE_MAPS_API_KEY ?= $(VITE_GOOGLE_MAPS_API_KEY)
SEED_DEVELOPMENT_USERS ?= false

MONGO_CONNECTION_STRING ?= mongodb://$(MONGO_ROOT_USERNAME):$(MONGO_ROOT_PASSWORD)@localhost:$(MONGO_PORT)/$(MONGO_DATABASE)?authSource=admin

export ASPNETCORE_ENVIRONMENT ASPNETCORE_URLS BASE_URL MONGO_CONNECTION_STRING MONGO_DATABASE MONGO_USERS_COLLECTION MONGO_ENERGY_RESERVATIONS_COLLECTION MONGO_INITIALIZE_ON_STARTUP JWT_ISSUER JWT_AUDIENCE JWT_SIGNING_KEY JWT_EXPIRES_MINUTES WEB_APP_ORIGIN WEB_APP_DOCKER_ORIGIN VITE_API_BASE_URL VITE_API_PROXY_TARGET VITE_GOOGLE_MAPS_API_KEY VITE_GOOGLE_MAPS_MAP_ID MOBILE_API_BASE_URL MOBILE_MAPS_API_KEY SEED_DEVELOPMENT_USERS BACKOFFICE_SEED_FIRST_NAME BACKOFFICE_SEED_LAST_NAME BACKOFFICE_SEED_EMAIL BACKOFFICE_SEED_PASSWORD GRID_OPERATOR_SEED_FIRST_NAME GRID_OPERATOR_SEED_LAST_NAME GRID_OPERATOR_SEED_EMAIL GRID_OPERATOR_SEED_PASSWORD SOLGRID_MONGO_TEST_CONNECTION_STRING

.PHONY: help env-example env-init check-runtime-secrets secrets-init secrets-set secrets-list secrets-clear docker-up docker-down docker-ps docker-logs docker-build docker-test-mongo-up docker-test-mongo-down restore build test verify clean format run health openapi login-backoffice login-grid-operator web-env web-install web-dev web-build mobile-env mobile-build mobile-install mobile-test mobile-clean

help: ## Show available commands.
	@awk 'BEGIN {FS = ":.*##"}; /^[a-zA-Z0-9_-]+:.*##/ {printf "  %-26s %s\n", $$1, $$2}' $(MAKEFILE_LIST)

env-example: ## Create .env from .env.example if it does not exist.
	@if [[ -e .env ]]; then echo '.env already exists; preserving its contents.'; else cp .env.example .env; chmod 600 .env; echo '.env created. Fill secret values before running Docker or secrets-set.'; fi

env-init: ## Create .env with locally generated MongoDB and JWT secrets.
	@if [[ -e .env ]]; then echo '.env already exists; preserving its contents.'; else \
		mongo_password="$$(openssl rand -hex 32)"; jwt_key="$$(openssl rand -hex 64)"; \
		sed -e "s/^MONGO_ROOT_PASSWORD=.*/MONGO_ROOT_PASSWORD=$$mongo_password/" -e "s/^JWT_SIGNING_KEY=.*/JWT_SIGNING_KEY=$$jwt_key/" .env.example > .env; \
		chmod 600 .env; echo '.env created with generated local MongoDB and JWT secrets.'; \
	fi

check-runtime-secrets: ## Verify required Docker/runtime secrets are configured.
	@test -f .env || { echo 'Create .env first: make env-init'; exit 1; }
	@test -n "$(MONGO_ROOT_PASSWORD)" || { echo 'Set MONGO_ROOT_PASSWORD in .env'; exit 1; }
	@test -n "$(JWT_SIGNING_KEY)" || { echo 'Set JWT_SIGNING_KEY in .env'; exit 1; }
	@test "$$(printf %s '$(JWT_SIGNING_KEY)' | wc -c | tr -d ' ')" -ge 32 || { echo 'JWT_SIGNING_KEY must be at least 32 bytes'; exit 1; }

secrets-init: ## Initialize .NET user-secrets for local API development.
	dotnet user-secrets init --project $(API_PROJECT)

secrets-set: check-runtime-secrets ## Store root .env values in .NET user-secrets.
	dotnet user-secrets set 'MongoDb:ConnectionString' '$(MONGO_CONNECTION_STRING)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:DatabaseName' '$(MONGO_DATABASE)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:UsersCollectionName' '$(MONGO_USERS_COLLECTION)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:EnergyReservationsCollectionName' '$(MONGO_ENERGY_RESERVATIONS_COLLECTION)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:InitializeOnStartup' '$(MONGO_INITIALIZE_ON_STARTUP)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:SeedDevelopmentUsers' '$(SEED_DEVELOPMENT_USERS)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:BackofficeSeedUser:FirstName' '$(BACKOFFICE_SEED_FIRST_NAME)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:BackofficeSeedUser:LastName' '$(BACKOFFICE_SEED_LAST_NAME)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:BackofficeSeedUser:Email' '$(BACKOFFICE_SEED_EMAIL)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:BackofficeSeedUser:Password' '$(BACKOFFICE_SEED_PASSWORD)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:GridOperatorSeedUser:FirstName' '$(GRID_OPERATOR_SEED_FIRST_NAME)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:GridOperatorSeedUser:LastName' '$(GRID_OPERATOR_SEED_LAST_NAME)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:GridOperatorSeedUser:Email' '$(GRID_OPERATOR_SEED_EMAIL)' --project $(API_PROJECT)
	dotnet user-secrets set 'MongoDb:GridOperatorSeedUser:Password' '$(GRID_OPERATOR_SEED_PASSWORD)' --project $(API_PROJECT)
	dotnet user-secrets set 'Jwt:Issuer' '$(JWT_ISSUER)' --project $(API_PROJECT)
	dotnet user-secrets set 'Jwt:Audience' '$(JWT_AUDIENCE)' --project $(API_PROJECT)
	dotnet user-secrets set 'Jwt:SigningKey' '$(JWT_SIGNING_KEY)' --project $(API_PROJECT)
	dotnet user-secrets set 'Jwt:ExpiresMinutes' '$(JWT_EXPIRES_MINUTES)' --project $(API_PROJECT)
	dotnet user-secrets set 'Cors:AllowedOrigins:0' '$(WEB_APP_ORIGIN)' --project $(API_PROJECT)
	dotnet user-secrets set 'Cors:AllowedOrigins:1' '$(WEB_APP_DOCKER_ORIGIN)' --project $(API_PROJECT)
	@echo 'API user-secrets updated from root .env.'

secrets-list: ## List API user-secrets.
	dotnet user-secrets list --project $(API_PROJECT)

secrets-clear: ## Clear API user-secrets.
	dotnet user-secrets clear --project $(API_PROJECT)

docker-up: check-runtime-secrets ## Start MongoDB, backend, and web app.
	docker compose --env-file .env up -d --build

docker-down: ## Stop Docker Compose services.
	docker compose --env-file .env down

docker-ps: ## Show Docker Compose service status.
	docker compose --env-file .env ps

docker-logs: ## Follow backend and MongoDB logs.
	docker compose --env-file .env logs -f mongo backend

docker-build: check-runtime-secrets ## Build the backend Docker image.
	docker compose --env-file .env build backend

docker-test-mongo-up: ## Start MongoDB for repository integration tests.
	docker compose -f docker-compose.test.yml up -d

docker-test-mongo-down: ## Stop the test MongoDB service.
	docker compose -f docker-compose.test.yml down

restore: ## Restore backend dependencies.
	dotnet restore $(API_PROJECT)
	dotnet restore $(DOMAIN_TESTS)
	dotnet restore $(APPLICATION_TESTS)
	dotnet restore $(API_TESTS)
	dotnet restore $(INFRA_TESTS)

build: restore ## Restore and build the backend API.
	dotnet build $(API_PROJECT) $(DOTNET_FLAGS)

test: restore ## Restore and run all backend test projects.
	dotnet test $(DOMAIN_TESTS) $(DOTNET_FLAGS)
	dotnet test $(APPLICATION_TESTS) $(DOTNET_FLAGS)
	dotnet test $(API_TESTS) $(DOTNET_FLAGS)
	dotnet test $(INFRA_TESTS) $(DOTNET_FLAGS)

verify: build test ## Build and test the backend.

clean: ## Clean backend build outputs.
	dotnet clean $(API_PROJECT) -v minimal
	dotnet clean $(DOMAIN_TESTS) -v minimal
	dotnet clean $(APPLICATION_TESTS) -v minimal
	dotnet clean $(API_TESTS) -v minimal
	dotnet clean $(INFRA_TESTS) -v minimal

format: ## Format backend projects.
	dotnet format $(API_PROJECT) --no-restore
	dotnet format $(DOMAIN_TESTS) --no-restore
	dotnet format $(APPLICATION_TESTS) --no-restore
	dotnet format $(API_TESTS) --no-restore
	dotnet format $(INFRA_TESTS) --no-restore

run: ## Run the API locally using .NET user-secrets.
	dotnet run --project $(API_PROJECT) --no-launch-profile

health: ## Request the backend health endpoint.
	curl --fail-with-body $(BASE_URL)/health

openapi: ## Request the OpenAPI document.
	curl --fail-with-body $(BASE_URL)/openapi/v1.json

login-backoffice: ## Login with the configured Backoffice development seed account.
	@test -n "$(BACKOFFICE_SEED_EMAIL)" && test -n "$(BACKOFFICE_SEED_PASSWORD)" || { echo 'Set BACKOFFICE_SEED_EMAIL and BACKOFFICE_SEED_PASSWORD in .env'; exit 1; }
	curl --fail-with-body -X POST $(BASE_URL)/api/v1/auth/login -H 'Content-Type: application/json' -d '{"email":"$(BACKOFFICE_SEED_EMAIL)","password":"$(BACKOFFICE_SEED_PASSWORD)"}'

login-grid-operator: ## Login with the configured GridOperator development seed account.
	@test -n "$(GRID_OPERATOR_SEED_EMAIL)" && test -n "$(GRID_OPERATOR_SEED_PASSWORD)" || { echo 'Set GRID_OPERATOR_SEED_EMAIL and GRID_OPERATOR_SEED_PASSWORD in .env'; exit 1; }
	curl --fail-with-body -X POST $(BASE_URL)/api/v1/auth/login -H 'Content-Type: application/json' -d '{"email":"$(GRID_OPERATOR_SEED_EMAIL)","password":"$(GRID_OPERATOR_SEED_PASSWORD)"}'

web-env: ## Generate ignored Vite configuration from root .env.
	@case '$(VITE_API_BASE_URL)' in http://*|https://*) ;; *) echo 'VITE_API_BASE_URL must be an HTTP(S) API base URL'; exit 1;; esac
	@case '$(VITE_API_BASE_URL)' in */health|*/health/) echo 'VITE_API_BASE_URL must be the API base URL, not the /health endpoint'; exit 1;; esac
	@{ \
		echo '# Generated by make web-env; edit root .env instead.'; \
		printf 'VITE_API_BASE_URL=%s\n' '$(VITE_API_BASE_URL)'; \
		printf 'VITE_API_PROXY_TARGET=%s\n' '$(VITE_API_PROXY_TARGET)'; \
		printf 'VITE_GOOGLE_MAPS_API_KEY=%s\n' '$(VITE_GOOGLE_MAPS_API_KEY)'; \
		printf 'VITE_GOOGLE_MAPS_MAP_ID=%s\n' '$(VITE_GOOGLE_MAPS_MAP_ID)'; \
	} > web-app/.env.local
	@chmod 600 web-app/.env.local
	@echo 'Web application configuration updated from root .env.'

web-install: ## Install React dependencies.
	npm --prefix web-app install

web-dev: web-env ## Start the React development server.
	npm --prefix web-app run dev

web-build: web-env ## Build the React application.
	npm --prefix web-app run build

mobile-env: ## Generate ignored Android configuration from root .env.
	@[[ '$(MOBILE_API_BASE_URL)' =~ ^https?://[^[:space:]?#]+/$$ ]] || { echo 'MOBILE_API_BASE_URL must be an HTTP(S) URL ending with /'; exit 1; }
	@{ \
		echo '# Generated by make mobile-env; edit root .env instead.'; \
		printf 'API_BASE_URL=%s\n' '$(MOBILE_API_BASE_URL)'; \
		printf 'MAPS_API_KEY=%s\n' '$(MOBILE_MAPS_API_KEY)'; \
	} > mobile-app/env.properties
	@chmod 600 mobile-app/env.properties
	@echo 'Android configuration updated from root .env.'

mobile-build: mobile-env ## Build the Android debug APK.
	cd mobile-app && ./gradlew :app:assembleDebug

mobile-install: mobile-env ## Build and install the Android debug APK.
	cd mobile-app && ./gradlew :app:installDebug

mobile-test: mobile-env ## Run Android JVM unit tests.
	cd mobile-app && ./gradlew :app:testDebugUnitTest

mobile-clean: ## Clean Android build outputs.
	cd mobile-app && ./gradlew clean
