#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# ClaudyGod — server-side deploy script
#
# Usage:
#   ./deploy.sh              # pull latest images & restart all services
#   ./deploy.sh sha-abc123   # pin to a specific image tag
#   ./deploy.sh --web-only   # restart only the web service
#   ./deploy.sh --api-only   # restart only the api (+ migration)
#
# Prerequisites on the server:
#   - Docker + Docker Compose plugin installed
#   - docker/.env file filled in (cp .env.example .env)
#   - External network exists: docker network create traefik-public
#   - Logged in to GHCR: echo TOKEN | docker login ghcr.io -u USER --password-stdin
# ─────────────────────────────────────────────────────────────────────────────

set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SCRIPT_DIR"

# ── Colours ──────────────────────────────────────────────────────────────────
GREEN='\033[0;32m'; YELLOW='\033[1;33m'; RED='\033[0;31m'; NC='\033[0m'
info()    { echo -e "${GREEN}→${NC} $*"; }
warn()    { echo -e "${YELLOW}⚠${NC}  $*"; }
success() { echo -e "${GREEN}✓${NC} $*"; }
die()     { echo -e "${RED}✗${NC} $*" >&2; exit 1; }

# ── Defaults ─────────────────────────────────────────────────────────────────
TAG="${1:-latest}"
MODE="all"

if [[ "${1:-}" == "--web-only" ]]; then MODE="web"; TAG="latest"; fi
if [[ "${1:-}" == "--api-only" ]]; then MODE="api"; TAG="latest"; fi

export TAG

# ── Ensure .env exists ───────────────────────────────────────────────────────
[[ -f .env ]] || die ".env not found. Run: cp .env.example .env && fill in values."

# ── Ensure external network exists ───────────────────────────────────────────
if ! docker network inspect traefik-public &>/dev/null; then
  warn "traefik-public network missing — creating it now."
  docker network create traefik-public
fi

echo ""
info "ClaudyGod deploy — tag: ${TAG}, mode: ${MODE}"
echo "────────────────────────────────────────────────"

# ── Pull latest images ────────────────────────────────────────────────────────
case "$MODE" in
  all)
    info "Pulling all images..."
    docker compose pull traefik redis db api migrate web
    ;;
  api)
    info "Pulling API image..."
    docker compose pull api migrate
    ;;
  web)
    info "Pulling web image..."
    docker compose pull web
    ;;
esac

# ── Start infrastructure first (idempotent) ───────────────────────────────────
if [[ "$MODE" == "all" ]]; then
  info "Starting infrastructure (traefik, redis, db)..."
  docker compose up -d --no-deps traefik redis db
  info "Waiting for database to be healthy..."
  docker compose up --no-deps --wait db
fi

# ── Run migrations ────────────────────────────────────────────────────────────
if [[ "$MODE" == "all" || "$MODE" == "api" ]]; then
  info "Running database migrations..."
  docker compose up --no-deps --remove-orphans migrate
  docker compose wait migrate || die "Migration failed. Aborting deploy."
  success "Migrations applied."
fi

# ── Start/restart application services ───────────────────────────────────────
case "$MODE" in
  all)
    info "Starting API..."
    docker compose up -d --no-deps --remove-orphans api
    info "Waiting for API to be healthy..."
    docker compose up --no-deps --wait api

    info "Starting web..."
    docker compose up -d --no-deps --remove-orphans web
    ;;
  api)
    info "Restarting API..."
    docker compose up -d --no-deps --remove-orphans api
    ;;
  web)
    info "Restarting web..."
    docker compose up -d --no-deps --remove-orphans web
    ;;
esac

# ── Prune unused images (save disk space) ────────────────────────────────────
info "Pruning unused images..."
docker image prune -f --filter "until=24h" >/dev/null

# ── Status ───────────────────────────────────────────────────────────────────
echo ""
docker compose ps
echo ""
success "Deploy complete — $(date -u +%Y-%m-%dT%H:%M:%SZ)"
