# CI/CD & Deployment Guide

## How it works

```
git push → GitHub Actions → builds Docker image → pushes to GHCR
                                                       │
                                                       ▼
                                   ClaudyGodweb-Infrastructure repo
                                   (separate repo — the actual deployment
                                   source of truth: docker-compose.yml,
                                   Traefik config, .env, Makefile)
```

**This repo (`CGM-Backend`) only builds and pushes the API image to GHCR.** It does not contain the production deployment configuration — that lives entirely in the **`ClaudyGodweb-Infrastructure`** repo, deployed on the server at `~/apps/claudygod/ClaudyGodweb-Infrastructure`.

If you're looking for `docker-compose.yml`, Traefik config, or the real `.env`, they are **not in this repo** — go to `ClaudyGodweb-Infrastructure`.

---

## GitHub Secrets to configure (this repo)

Go to **Settings → Secrets and variables → Actions**:

| Secret | Value |
|--------|-------|
| `VPS_HOST` | Server IP or hostname |
| `VPS_USER` | SSH username |
| `VPS_SSH_KEY` | Full private key (`-----BEGIN ... -----END ...`) |
| `VPS_PORT` | SSH port (default `22`) |
| `VPS_DEPLOY_PATH` | Absolute path to `ClaudyGodweb-Infrastructure` on the server (e.g. `/home/server/apps/claudygod/ClaudyGodweb-Infrastructure`) |
| `MIGRATE_CONNECTION_STRING` | Supabase connection string used to run EF Core migrations from the CI runner |

`GITHUB_TOKEN` (automatic, no setup needed) handles both repo checkout and GHCR push — no custom PAT required.

---

## What the `deploy` job actually does

On every push to `main`, after build+push succeeds, GitHub Actions SSHs into the server and runs, inside `VPS_DEPLOY_PATH`:

```bash
docker compose --env-file <path>/.env --project-directory <path> -f <path>/docker/docker-compose.yml pull api
docker compose ... up -d --no-deps --remove-orphans --scale api=2 api
```

This only touches the `api` service (2 replicas) — it does not restart `web`, `redis`, or `migrate`. Migrations run separately, from the CI runner directly against Supabase (see the `build` job's "Run database migrations" step) — not on the server.

## Manual deploy (on the server)

Real, current deployment for the whole stack (`api`, `web`, `redis`, `grafana`, `migrate`) is manual, via `ClaudyGodweb-Infrastructure`'s own tooling:

```bash
cd ~/apps/claudygod/ClaudyGodweb-Infrastructure
make deploy
```

See that repo's own `README.md` / `DEPLOYMENT_GUIDE.md` for the full runbook, environment variables, and rollback steps.

---

## Image tags produced by CI

| Tag | When | Use for |
|-----|------|---------|
| `latest` | Every push to `main` | Rolling deploys |
| `sha-xxxxxxx` | Every push | Pinned/rollback deploys |
| `pr-N` | Pull requests | Preview builds (not pushed) |
