# Optional GNU Make shortcuts. Native Windows users can run node scripts/tasks.mjs <task>.
# Configuration is read by Node, so .env contents never become shell commands.
NODE ?= node
.DEFAULT_GOAL := help
TASKS := help env-example env-init check-runtime-secrets secrets-init secrets-set secrets-list secrets-clear docker-up docker-down docker-ps docker-logs docker-build docker-test-mongo-up docker-test-mongo-down restore build test verify clean format run health openapi login-backoffice login-grid-operator web-install web-dev web-build

.PHONY: $(TASKS)
$(TASKS):
	@$(NODE) scripts/tasks.mjs $@
