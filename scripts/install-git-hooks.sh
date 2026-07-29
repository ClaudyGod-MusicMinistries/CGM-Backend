#!/usr/bin/env bash
set -euo pipefail

repo_root="$(git rev-parse --show-toplevel 2>/dev/null)" || {
  echo "✗ Not inside a Git repository." >&2
  exit 1
}

cd "$repo_root"
git config core.hooksPath .githooks
chmod +x .githooks/pre-commit .githooks/pre-push

configured="$(git config --get core.hooksPath)"
[[ "$configured" == ".githooks" ]] || {
  echo "✗ Git hook activation verification failed." >&2
  exit 1
}

echo "✓ Backend Git hooks installed from $repo_root/.githooks"
