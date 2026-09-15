SHELL := /bin/bash

ifneq (,$(wildcard .env))
include .env
export
endif

API_DIR := web-service
API_PROJECT := $(API_DIR)/src/SolGrid.Api/SolGrid.Api.csproj
PROJECTS := $(shell find $(API_DIR)/src -name '*.csproj' -print)
TEST_PROJECTS := $(shell find $(API_DIR)/tests -name '*.csproj' -print)

BASE_URL ?= http://localhost:5186
ASPNETCORE_URLS ?= $(BASE_URL)
ASPNETCORE_ENVIRONMENT ?= Development

MONGO_CONNECTION_STRING ?= mongodb://localhost:27017
MONGO_DATABASE_NAME ?= SolGrid
MONGO_USERS_COLLECTION_NAME ?= Users
MONGO_SEED_DEVELOPMENT_USERS ?= false

JWT_ISSUER ?= SolGrid
JWT_AUDIENCE ?= SolGrid.Web
JWT_SIGNING_KEY ?=
JWT_EXPIRES_MINUTES ?= 60

SEED_BACKOFFICE_EMAIL ?=
SEED_BACKOFFICE_PASSWORD ?=
SEED_GRID_OPERATOR_EMAIL ?=
SEED_GRID_OPERATOR_PASSWORD ?=

.PHONY: help env-example restore build test verify clean format run openapi login-backoffice login-grid-operator

help:
	@echo "Available commands:"
	@echo "  make env-example       Create .env from .env.example if it exists"
	@echo "  make restore           Restore .NET packages"
	@echo "  make build             Build the SolGrid API and referenced projects"
	@echo "  make test              Run all .NET tests"
	@echo "  make verify            Build and test the backend"
	@echo "  make clean             Clean all .NET projects"
	@echo "  make format            Run dotnet format on all projects"
	@echo "  make run               Run the API with local MongoDB and JWT settings"
	@echo "  make openapi           Check the running API OpenAPI document"
	@echo "  make login-backoffice  Login using the configured Backoffice seed account"
	@echo "  make login-grid-operator  Login using the configured Grid Operator seed account"

env-example:
	@if test -f .env.example; then test -f .env || cp .env.example .env; echo ".env is ready."; else echo ".env.example does not exist; create .env with the variables documented in the Makefile."; fi

restore:
	@set -e; for project in $(PROJECTS) $(TEST_PROJECTS); do dotnet restore "$$project"; done

build:
	@set -e; for project in $(PROJECTS); do dotnet build "$$project" --no-restore -v minimal; done

test:
	@set -e; for project in $(TEST_PROJECTS); do dotnet test "$$project" --no-restore -v minimal; done

verify: build test

clean:
	@set -e; for project in $(PROJECTS) $(TEST_PROJECTS); do dotnet clean "$$project" -v minimal; done

format:
	@set -e; for project in $(PROJECTS) $(TEST_PROJECTS); do dotnet format "$$project" --no-restore; done

run:
	@test -n "$(JWT_SIGNING_KEY)" || (echo "Set JWT_SIGNING_KEY to a value of at least 32 bytes" && exit 1)
	@cd $(API_DIR) && \
	ASPNETCORE_ENVIRONMENT="$(ASPNETCORE_ENVIRONMENT)" \
	ASPNETCORE_URLS="$(ASPNETCORE_URLS)" \
	MongoDb__ConnectionString="$(MONGO_CONNECTION_STRING)" \
	MongoDb__DatabaseName="$(MONGO_DATABASE_NAME)" \
	MongoDb__UsersCollectionName="$(MONGO_USERS_COLLECTION_NAME)" \
	MongoDb__SeedDevelopmentUsers="$(MONGO_SEED_DEVELOPMENT_USERS)" \
	MongoDb__BackofficeSeedUser__Email="$(SEED_BACKOFFICE_EMAIL)" \
	MongoDb__BackofficeSeedUser__Password="$(SEED_BACKOFFICE_PASSWORD)" \
	MongoDb__GridOperatorSeedUser__Email="$(SEED_GRID_OPERATOR_EMAIL)" \
	MongoDb__GridOperatorSeedUser__Password="$(SEED_GRID_OPERATOR_PASSWORD)" \
	Jwt__Issuer="$(JWT_ISSUER)" \
	Jwt__Audience="$(JWT_AUDIENCE)" \
	Jwt__SigningKey="$(JWT_SIGNING_KEY)" \
	Jwt__ExpiresMinutes="$(JWT_EXPIRES_MINUTES)" \
	dotnet run --project src/SolGrid.Api/SolGrid.Api.csproj --no-launch-profile

openapi:
	curl --fail --show-error --silent "$(BASE_URL)/openapi/v1.json"

login-backoffice:
	@test -n "$(SEED_BACKOFFICE_EMAIL)" || (echo "Set SEED_BACKOFFICE_EMAIL" && exit 1)
	@test -n "$(SEED_BACKOFFICE_PASSWORD)" || (echo "Set SEED_BACKOFFICE_PASSWORD" && exit 1)
	@curl --fail --show-error -H "Content-Type: application/json" \
		-d '{"email":"$(SEED_BACKOFFICE_EMAIL)","password":"$(SEED_BACKOFFICE_PASSWORD)"}' \
		"$(BASE_URL)/api/v1/auth/login"

login-grid-operator:
	@test -n "$(SEED_GRID_OPERATOR_EMAIL)" || (echo "Set SEED_GRID_OPERATOR_EMAIL" && exit 1)
	@test -n "$(SEED_GRID_OPERATOR_PASSWORD)" || (echo "Set SEED_GRID_OPERATOR_PASSWORD" && exit 1)
	@curl --fail --show-error -H "Content-Type: application/json" \
		-d '{"email":"$(SEED_GRID_OPERATOR_EMAIL)","password":"$(SEED_GRID_OPERATOR_PASSWORD)"}' \
		"$(BASE_URL)/api/v1/auth/login"
