# CI/CD & Deployment Guide

## How it works

```
git push → GitHub Actions → builds Docker image → pushes to GHCR → SSHs server → deploys
```

Every push to `main` triggers the pipeline automatically.

---

## GitHub Secrets to configure

Go to **Settings → Secrets and variables → Actions** in each repo and add:

### Required secrets (both repos)

| Secret | Value |
|--------|-------|
| `SSH_HOST` | Your server IP or hostname |
| `SSH_USER` | SSH username (e.g. `ubuntu`) |
| `SSH_PRIVATE_KEY` | Full private key (`-----BEGIN ... -----END ...`) |
| `SSH_PORT` | SSH port (default `22`) |
| `DEPLOY_PATH` | Absolute path to the `docker/` folder on server (e.g. `/opt/claudygod/docker`) |

### Required variables (website2.0 repo only)

Go to **Settings → Secrets and variables → Actions → Variables** tab:

| Variable | Value |
|----------|-------|
| `NEXT_PUBLIC_API_URL` | `https://api.claudygod.org` |
| `NEXT_PUBLIC_SITE_URL` | `https://claudygod.org` |

> Variables (not secrets) are fine for public Next.js env vars — they are baked into the image at build time.

---

## GHCR Package visibility

After the first push, go to:  
`https://github.com/orgs/ClaudyGod-MusicMinistries/packages`

Set both packages (`cgm-api`, `cgm-web`) to **Public** — or keep Private and ensure the server has GHCR login configured.

**Log in on the server once:**
```bash
echo YOUR_GITHUB_PAT | docker login ghcr.io -u YOUR_GITHUB_USERNAME --password-stdin
```
Use a GitHub PAT with `read:packages` scope. Store the credential permanently — Docker saves it in `~/.docker/config.json`.

---

## Server first-time setup

```bash
# 1. Create external network
docker network create traefik-public

# 2. Clone the backend repo
git clone https://github.com/ClaudyGod-MusicMinistries/CGM-Backend.git /opt/claudygod
cd /opt/claudygod/docker

# 3. Fill in secrets
cp .env.example .env
nano .env   # fill every CHANGE_ME

# 4. Login to GHCR
echo YOUR_PAT | docker login ghcr.io -u YOUR_USERNAME --password-stdin

# 5. First deploy
./deploy.sh
```

---

## Manual deploy / rollback

```bash
cd /opt/claudygod/docker

# Deploy latest
./deploy.sh

# Pin to a specific image (rollback)
./deploy.sh sha-a1b2c3d

# Restart only the web service
./deploy.sh --web-only

# Restart only the API
./deploy.sh --api-only
```

---

## Image tags produced by CI

| Tag | When | Use for |
|-----|------|---------|
| `latest` | Every push to `main` | Rolling deploys |
| `sha-xxxxxxx` | Every push | Pinned/rollback deploys |
| `pr-N` | Pull requests | Preview builds (not pushed) |
