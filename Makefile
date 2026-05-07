# ─────────────────────────────────────────────────────────────────────────────
#  PigeonRacing Platform — Makefile
#  Common docker compose shortcuts so you don't have to remember long commands.
# ─────────────────────────────────────────────────────────────────────────────

.DEFAULT_GOAL := help
.PHONY: help up down dev prod build logs shell migrate seed test clean reset

COMPOSE      := docker compose
COMPOSE_PROD := docker compose -f docker-compose.yml -f docker-compose.prod.yml
API_SERVICE  := api

## ── Help ────────────────────────────────────────────────────────────────────

help: ## Show this help
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) \
	  | awk 'BEGIN {FS = ":.*?## "}; {printf "  \033[36m%-18s\033[0m %s\n", $$1, $$2}'

## ── Development ──────────────────────────────────────────────────────────────

dev: ## Start full stack in development mode (hot reload)
	@echo "Starting PigeonRacing in development mode…"
	$(COMPOSE) up -d
	@echo ""
	@echo "  API:        http://localhost:5000/swagger"
	@echo "  UI:         http://localhost:4200"
	@echo "  Grafana:    http://localhost:3000  (admin/admin)"
	@echo "  Prometheus: http://localhost:9090"
	@echo ""

up: dev ## Alias for dev

down: ## Stop all containers
	$(COMPOSE) down

## ── Production ───────────────────────────────────────────────────────────────

prod: ## Start full stack in production mode (resource limits, no dev ports)
	@[ -f .env ] || { echo "ERROR: .env file not found. Copy .env.example and fill in values."; exit 1; }
	@echo "Starting PigeonRacing in production mode…"
	$(COMPOSE_PROD) up -d --remove-orphans
	@echo "Done. Grafana: http://localhost:3000"

prod-build: ## Rebuild production images and restart
	$(COMPOSE_PROD) up -d --build --remove-orphans

## ── Build ────────────────────────────────────────────────────────────────────

build: ## Build all Docker images
	$(COMPOSE) build --parallel

build-api: ## Build only the API image
	$(COMPOSE) build api

build-ui: ## Build only the UI image
	$(COMPOSE) build ui

## ── Database ─────────────────────────────────────────────────────────────────

migrate: ## Run EF Core database migrations
	@echo "Running migrations…"
	$(COMPOSE) exec $(API_SERVICE) dotnet ef database update \
	  --project PigeonRacing.Infrastructure \
	  --startup-project PigeonRacing.API \
	  --no-build

db-shell: ## Open SQL Server interactive shell
	$(COMPOSE) exec sqlserver /opt/mssql-tools/bin/sqlcmd \
	  -S localhost -U sa -P "$${SQL_PASSWORD:-Str0ngP@ssword!}"

redis-shell: ## Open Redis interactive shell
	$(COMPOSE) exec redis redis-cli

## ── Logs ─────────────────────────────────────────────────────────────────────

logs: ## Follow logs from all services
	$(COMPOSE) logs -f

logs-api: ## Follow API logs only
	$(COMPOSE) logs -f api

logs-ui: ## Follow UI logs only
	$(COMPOSE) logs -f ui

logs-db: ## Follow SQL Server logs
	$(COMPOSE) logs -f sqlserver

## ── Shell access ─────────────────────────────────────────────────────────────

shell-api: ## Open shell inside API container
	$(COMPOSE) exec $(API_SERVICE) /bin/bash

shell-ui: ## Open shell inside UI container
	$(COMPOSE) exec ui /bin/sh

## ── Health checks ────────────────────────────────────────────────────────────

health: ## Check health of all services
	@echo "=== Service health ==="
	@$(COMPOSE) ps --format "table {{.Name}}\t{{.Status}}\t{{.Ports}}"
	@echo ""
	@echo "=== API health ==="
	@curl -sf http://localhost:5000/health | python3 -m json.tool 2>/dev/null || echo "API not reachable"

status: ## Show running containers
	$(COMPOSE) ps

## ── Cleanup ──────────────────────────────────────────────────────────────────

clean: ## Stop containers and remove containers/networks (keeps volumes)
	$(COMPOSE) down --remove-orphans

reset: ## ⚠️  DESTRUCTIVE: stop, remove containers, images, AND volumes
	@echo "WARNING: This will delete all data including the database!"
	@read -p "Type 'yes' to continue: " confirm && [ "$$confirm" = "yes" ]
	$(COMPOSE) down -v --remove-orphans --rmi local
	@echo "All containers, images, and volumes removed."

prune: ## Remove unused Docker resources (system-wide)
	docker system prune -f
	docker volume prune -f

## ── Environment setup ────────────────────────────────────────────────────────

env-setup: ## Copy .env.example to .env (won't overwrite existing)
	@[ -f .env ] && echo ".env already exists — skipping." || \
	  (cp .env.example .env && echo "Created .env from .env.example. Edit it before starting.")

## ── Quick start ──────────────────────────────────────────────────────────────

quickstart: env-setup dev ## First-time setup: create .env then start dev stack
	@echo "Waiting for SQL Server to be ready (may take 60s)…"
	@sleep 20
	@echo "If the API fails to start, run: make migrate"
