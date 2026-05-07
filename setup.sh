#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
#  PigeonRacing Platform — First-Time Setup Script
#  Usage: ./setup.sh
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
BOLD='\033[1m'
NC='\033[0m'

log()    { echo -e "${BLUE}▶${NC} $*"; }
ok()     { echo -e "${GREEN}✓${NC} $*"; }
warn()   { echo -e "${YELLOW}⚠${NC} $*"; }
err()    { echo -e "${RED}✗${NC} $*"; exit 1; }
header() { echo -e "\n${BOLD}$*${NC}"; }

header "🕊️  PigeonRacing Platform — Setup"

# ── Prerequisites check ───────────────────────────────────────────────────────

header "1. Checking prerequisites…"

check_cmd() {
  command -v "$1" &>/dev/null || err "$1 is required but not installed. See https://docs.docker.com/get-docker/"
}

check_cmd docker
ok "Docker found: $(docker --version)"

docker compose version &>/dev/null || err "Docker Compose v2 is required. Update Docker Desktop."
ok "Docker Compose found: $(docker compose version)"

# ── Environment file ──────────────────────────────────────────────────────────

header "2. Environment configuration…"

if [ -f .env ]; then
  warn ".env already exists — skipping creation. Edit it manually if needed."
else
  cp .env.example .env
  ok "Created .env from .env.example"

  # Generate a strong JWT key
  if command -v openssl &>/dev/null; then
    JWT_KEY=$(openssl rand -base64 48 | tr -d '=+/' | cut -c1-64)
    sed -i.bak "s/CHANGE_THIS_TO_A_STRONG_RANDOM_SECRET_KEY_MIN_32_CHARS/$JWT_KEY/" .env
    rm -f .env.bak
    ok "Generated random JWT key"
  else
    warn "openssl not found — please set JWT_KEY in .env manually before starting"
  fi
fi

echo ""
echo "  Edit ${BOLD}.env${NC} to configure:"
echo "   • SQL_PASSWORD (use something strong in production)"
echo "   • SLACK_WEBHOOK_URL (for alerts — optional)"
echo "   • GRAFANA_PASSWORD"
echo "   • Integration:PigeonLoftManagerApiKey (when PLM provides it)"
echo ""
read -p "Press Enter to continue with current .env values, or Ctrl+C to edit first…"

# ── Pull base images ──────────────────────────────────────────────────────────

header "3. Pulling base Docker images…"
log "This may take a few minutes on first run…"
docker compose pull --ignore-buildable 2>/dev/null || true
ok "Base images ready"

# ── Build application images ──────────────────────────────────────────────────

header "4. Building application images…"
log "Building API and UI (first build takes 3–5 minutes)…"
docker compose build --parallel
ok "Images built"

# ── Start infrastructure services ─────────────────────────────────────────────

header "5. Starting infrastructure (SQL Server + Redis)…"
docker compose up -d sqlserver redis
log "Waiting 45 seconds for SQL Server to initialise…"
sleep 45

# Wait for SQL Server health
RETRIES=10
while [ $RETRIES -gt 0 ]; do
  if docker compose exec -T sqlserver \
    /opt/mssql-tools/bin/sqlcmd -S localhost -U sa \
    -P "${SQL_PASSWORD:-Str0ngP@ssword!}" \
    -Q "SELECT 1" &>/dev/null 2>&1; then
    ok "SQL Server is ready"
    break
  fi
  RETRIES=$((RETRIES-1))
  log "SQL Server not ready yet, waiting 10s… ($RETRIES attempts left)"
  sleep 10
done

[ $RETRIES -eq 0 ] && err "SQL Server failed to start in time. Check: docker compose logs sqlserver"

# ── Start full stack ──────────────────────────────────────────────────────────

header "6. Starting all services…"
docker compose up -d
ok "All services started"

# ── Wait for API health ───────────────────────────────────────────────────────

header "7. Waiting for API to be healthy…"
RETRIES=20
while [ $RETRIES -gt 0 ]; do
  if curl -sf http://localhost:5000/health/live &>/dev/null; then
    ok "API is healthy"
    break
  fi
  RETRIES=$((RETRIES-1))
  log "API starting… ($RETRIES checks left)"
  sleep 5
done

[ $RETRIES -eq 0 ] && warn "API health check timed out. Check: docker compose logs api"

# ── Done ──────────────────────────────────────────────────────────────────────

header "✅  Setup complete!"
echo ""
echo -e "  ${BOLD}Application${NC}"
echo -e "  • UI:         ${BLUE}http://localhost:4200${NC}"
echo -e "  • API Swagger: ${BLUE}http://localhost:5000/swagger${NC}"
echo -e "  • API Health:  ${BLUE}http://localhost:5000/health${NC}"
echo ""
echo -e "  ${BOLD}Monitoring${NC}"
echo -e "  • Grafana:     ${BLUE}http://localhost:3000${NC}  (admin / \${GRAFANA_PASSWORD:-admin123})"
echo -e "  • Prometheus:  ${BLUE}http://localhost:9090${NC}"
echo -e "  • Alertmanager:${BLUE}http://localhost:9093${NC}"
echo ""
echo -e "  ${BOLD}Default credentials${NC} (change in .env before production)"
echo "  • API admin:    admin@pigeonracing.com / Admin123!"
echo ""
echo -e "  ${BOLD}Useful commands${NC}"
echo "  • make help        — show all commands"
echo "  • make logs        — follow all logs"
echo "  • make logs-api    — follow API logs"
echo "  • make down        — stop everything"
echo "  • make health      — check service health"
echo ""
