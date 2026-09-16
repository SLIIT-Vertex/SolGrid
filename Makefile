SHELL := /bin/bash

ifneq (,$(wildcard .env))
include .env
export
endif

API_DIR := web-service
WEB_DIR := web-app
API_PROJECT := $(API_DIR)/src/SolGrid.Api/SolGrid.Api.csproj
DOMAIN_TESTS := $(API_DIR)/tests/SolGrid.Domain.Tests/SolGrid.Domain.Tests.csproj
APPLICATION_TESTS := $(API_DIR)/tests/SolGrid.Application.Tests/SolGrid.Application.Tests.csproj
API_TESTS := $(API_DIR)/tests/SolGrid.Api.Tests/SolGrid.Api.Tests.csproj
INFRA_TESTS := $(API_DIR)/tests/SolGrid.Infrastructure.Tests/SolGrid.Infrastructure.Tests.csproj

BASE_URL ?= http://localhost:5000
ASPNETCORE_URLS ?= http://localhost:5000
ASPNETCORE_ENVIRONMENT ?= Development

MONGO_ROOT_USERNAME ?= solgrid_admin
MONGO_ROOT_PASSWORD ?=
MONGO_DATABASE ?= SolGrid
MONGO_USERS_COLLECTION ?= Users
MONGO_ENERGY_RESERVATIONS_COLLECTION ?= EnergyReservations
MONGO_INITIALIZE_ON_STARTUP ?= true
MONGO_PORT ?= 27017
MONGO_TEST_PORT ?= 27018
MONGO_CONNECTION_STRING ?= mongodb://$(MONGO_ROOT_USERNAME):$(MONGO_ROOT_PASSWORD)@localhost:$(MONGO_PORT)/$(MONGO_DATABASE)?authSource=admin

JWT_ISSUER ?= SolGrid
JWT_AUDIENCE ?= SolGrid.Web
JWT_SIGNING_KEY ?=
JWT_EXPIRES_MINUTES ?= 60

WEB_APP_ORIGIN ?= http://localhost:5173
WEB_APP_DOCKER_ORIGIN ?= http://localhost:8080

SEED_DEVELOPMENT_USERS ?= false
BACKOFFICE_SEED_FIRST_NAME ?= Backoffice
BACKOFFICE_SEED_LAST_NAME ?= Admin
BACKOFFICE_SEED_EMAIL ?=
BACKOFFICE_SEED_PASSWORD ?=
GRID_OPERATOR_SEED_FIRST_NAME ?= Grid
GRID_OPERATOR_SEED_LAST_NAME ?= Operator
GRID_OPERATOR_SEED_EMAIL ?=
GRID_OPERATOR_SEED_PASSWORD ?=

.PHONY: help env-example env-init check-runtime-secrets secrets-init secrets-set secrets-list secrets-clear docker-up docker-down docker-ps docker-logs docker-build docker-test-mongo-up docker-test-mongo-down restore build test verify clean format run health openapi login-backoffice login-grid-operator web-install web-dev web-build

help:
	@echo "Available commands:"
	@echo "  make env-example            Create .env from .env.example if missing"
	@echo "  make env-init               Create .env and generate local JWT/Mongo secrets"
	@echo "  make secrets-init           Initialize .NET user-secrets for the API"
	@echo "  make secrets-set            Store .env values in .NET user-secrets for local dotnet run"
	@echo "  make secrets-list           List configured API user-secrets keys"
	@echo "  make secrets-clear          Clear API user-secrets"
	@echo "  make docker-up              Start Mongo, backend, and web app with Docker Compose"
	@echo "  make docker-down            Stop Docker Compose services"
	@echo "  make docker-ps              Show Docker Compose service status"
	@echo "  make docker-logs            Follow backend and Mongo logs"
	@echo "  make docker-build           Build backend Docker image"
	@echo "  make docker-test-mongo-up   Start MongoDB for repository integration tests"
	@echo "  make docker-test-mongo-down Stop MongoDB test container"
	@echo "  make restore                Restore backend packages"
	@echo "  make build                  Build backend API"
	@echo "  make test                   Run backend tests"
	@echo "  make verify                 Build and test backend"
	@echo "  make clean                  Clean backend projects"
	@echo "  make format                 Format backend projects"
	@echo "  make run                    Run API locally using .NET user-secrets"
	@echo "  make health                 Check /health"
	@echo "  make openapi                Check OpenAPI JSON"
	@echo "  make login-backoffice       Login with configured Backoffice seed account"
	@echo "  make login-grid-operator    Login with configured GridOperator seed account"
	@echo "  make web-install            Install React dependencies"
	@echo "  make web-dev                Start React dev server"
	@echo "  make web-build              Build React app"

env-example:
	@test -f .env || cp .env.example .env
	@echo ".env is ready. Fill secret values before running Docker or make secrets-set."

env-init:
	@test ! -f .env || (echo ".env already exists; not overwriting secrets." && exit 0)
	@mongo_password=$$(openssl rand -hex 32); \
	jwt_key=$$(openssl rand -hex 64); \
	sed \
		-e "s|^MONGO_ROOT_PASSWORD=.*|MONGO_ROOT_PASSWORD=$$mongo_password|" \
		-e "s|^JWT_SIGNING_KEY=.*|JWT_SIGNING_KEY=$$jwt_key|" \
		.env.example > .env
	@chmod 600 .env
	@echo ".env created with generated local MongoDB and JWT secrets."

check-runtime-secrets:
	@test -f .env || (echo "Create .env first: make env-init" && exit 1)
	@test -n "$(MONGO_ROOT_PASSWORD)" || (echo "Set MONGO_ROOT_PASSWORD in .env" && exit 1)
	@test -n "$(JWT_SIGNING_KEY)" || (echo "Set JWT_SIGNING_KEY in .env" && exit 1)
	@test $$(printf "%s" "$(JWT_SIGNING_KEY)" | wc -c) -ge 32 || (echo "JWT_SIGNING_KEY must be at least 32 bytes" && exit 1)

secrets-init:
	@dotnet user-secrets init --project $(API_PROJECT)

secrets-set: check-runtime-secrets
	@dotnet user-secrets set "MongoDb:ConnectionString" "$(MONGO_CONNECTION_STRING)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:DatabaseName" "$(MONGO_DATABASE)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:UsersCollectionName" "$(MONGO_USERS_COLLECTION)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:EnergyReservationsCollectionName" "$(MONGO_ENERGY_RESERVATIONS_COLLECTION)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:InitializeOnStartup" "$(MONGO_INITIALIZE_ON_STARTUP)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:SeedDevelopmentUsers" "$(SEED_DEVELOPMENT_USERS)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:BackofficeSeedUser:FirstName" "$(BACKOFFICE_SEED_FIRST_NAME)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:BackofficeSeedUser:LastName" "$(BACKOFFICE_SEED_LAST_NAME)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:BackofficeSeedUser:Email" "$(BACKOFFICE_SEED_EMAIL)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:BackofficeSeedUser:Password" "$(BACKOFFICE_SEED_PASSWORD)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:GridOperatorSeedUser:FirstName" "$(GRID_OPERATOR_SEED_FIRST_NAME)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:GridOperatorSeedUser:LastName" "$(GRID_OPERATOR_SEED_LAST_NAME)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:GridOperatorSeedUser:Email" "$(GRID_OPERATOR_SEED_EMAIL)" --project $(API_PROJECT)
	@dotnet user-secrets set "MongoDb:GridOperatorSeedUser:Password" "$(GRID_OPERATOR_SEED_PASSWORD)" --project $(API_PROJECT)
	@dotnet user-secrets set "Jwt:Issuer" "$(JWT_ISSUER)" --project $(API_PROJECT)
	@dotnet user-secrets set "Jwt:Audience" "$(JWT_AUDIENCE)" --project $(API_PROJECT)
	@dotnet user-secrets set "Jwt:SigningKey" "$(JWT_SIGNING_KEY)" --project $(API_PROJECT)
	@dotnet user-secrets set "Jwt:ExpiresMinutes" "$(JWT_EXPIRES_MINUTES)" --project $(API_PROJECT)
	@dotnet user-secrets set "Cors:AllowedOrigins:0" "$(WEB_APP_ORIGIN)" --project $(API_PROJECT)
	@dotnet user-secrets set "Cors:AllowedOrigins:1" "$(WEB_APP_DOCKER_ORIGIN)" --project $(API_PROJECT)
	@echo "API user-secrets updated from .env."

secrets-list:
	@dotnet user-secrets list --project $(API_PROJECT)

secrets-clear:
	@dotnet user-secrets clear --project $(API_PROJECT)

docker-up: check-runtime-secrets
	@docker compose --env-file .env up -d --build

docker-down:
	@docker compose --env-file .env down

docker-ps:
	@docker compose --env-file .env ps

docker-logs:
	@docker compose --env-file .env logs -f mongo backend

docker-build: check-runtime-secrets
	@docker compose --env-file .env build backend

docker-test-mongo-up:
	@docker compose -f docker-compose.test.yml up -d

docker-test-mongo-down:
	@docker compose -f docker-compose.test.yml down

restore:
	@dotnet restore $(API_PROJECT)
	@dotnet restore $(DOMAIN_TESTS)
	@dotnet restore $(APPLICATION_TESTS)
	@dotnet restore $(API_TESTS)
	@dotnet restore $(INFRA_TESTS)

build:
	@dotnet build $(API_PROJECT) --no-restore -v minimal -m:1 -nr:false -p:UseSharedCompilation=false

test:
	@dotnet test $(DOMAIN_TESTS) --no-restore -v minimal -m:1 -nr:false -p:UseSharedCompilation=false
	@dotnet test $(APPLICATION_TESTS) --no-restore -v minimal -m:1 -nr:false -p:UseSharedCompilation=false
	@dotnet test $(API_TESTS) --no-restore -v minimal -m:1 -nr:false -p:UseSharedCompilation=false
	@dotnet test $(INFRA_TESTS) --no-restore -v minimal -m:1 -nr:false -p:UseSharedCompilation=false

verify: build test

clean:
	@dotnet clean $(API_PROJECT) -v minimal
	@dotnet clean $(DOMAIN_TESTS) -v minimal
	@dotnet clean $(APPLICATION_TESTS) -v minimal
	@dotnet clean $(API_TESTS) -v minimal
	@dotnet clean $(INFRA_TESTS) -v minimal

format:
	@dotnet format $(API_PROJECT) --no-restore
	@dotnet format $(DOMAIN_TESTS) --no-restore
	@dotnet format $(APPLICATION_TESTS) --no-restore
	@dotnet format $(API_TESTS) --no-restore
	@dotnet format $(INFRA_TESTS) --no-restore

run:
	@ASPNETCORE_ENVIRONMENT="$(ASPNETCORE_ENVIRONMENT)" \
	ASPNETCORE_URLS="$(ASPNETCORE_URLS)" \
	dotnet run --project $(API_PROJECT) --no-launch-profile

health:
	@curl --fail --show-error --silent "$(BASE_URL)/health"

openapi:
	@curl --fail --show-error --silent "$(BASE_URL)/openapi/v1.json"

login-backoffice:
	@test -n "$(BACKOFFICE_SEED_EMAIL)" || (echo "Set BACKOFFICE_SEED_EMAIL in .env" && exit 1)
	@test -n "$(BACKOFFICE_SEED_PASSWORD)" || (echo "Set BACKOFFICE_SEED_PASSWORD in .env" && exit 1)
	@curl --fail --show-error -H "Content-Type: application/json" \
		-d '{"email":"$(BACKOFFICE_SEED_EMAIL)","password":"$(BACKOFFICE_SEED_PASSWORD)"}' \
		"$(BASE_URL)/api/v1/auth/login"

login-grid-operator:
	@test -n "$(GRID_OPERATOR_SEED_EMAIL)" || (echo "Set GRID_OPERATOR_SEED_EMAIL in .env" && exit 1)
	@test -n "$(GRID_OPERATOR_SEED_PASSWORD)" || (echo "Set GRID_OPERATOR_SEED_PASSWORD in .env" && exit 1)
	@curl --fail --show-error -H "Content-Type: application/json" \
		-d '{"email":"$(GRID_OPERATOR_SEED_EMAIL)","password":"$(GRID_OPERATOR_SEED_PASSWORD)"}' \
		"$(BASE_URL)/api/v1/auth/login"

web-install:
	@cd $(WEB_DIR) && npm install

web-dev:
	@cd $(WEB_DIR) && npm run dev

web-build:
	@cd $(WEB_DIR) && npm run build
