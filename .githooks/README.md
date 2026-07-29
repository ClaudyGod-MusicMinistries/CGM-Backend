# Git hooks

Native git hooks — no Husky, no npm/NuGet package. Tracked in version control here instead of the untracked `.git/hooks/`, and wired up via git's built-in `core.hooksPath` setting.

## One-time setup (per clone)

```bash
./scripts/install-git-hooks.sh
```

That's it — every commit and push from then on runs the hooks below automatically.

## What runs

- **pre-commit** — staged whitespace/conflict-marker and credential checks, followed by C# formatting verification. It never rewrites staged work.
- **pre-push** — restore, formatting, vulnerability audit, Release build, EF migration-drift check, unit/API tests, and disposable PostgreSQL integration tests. Docker must be running.

## Bypassing (use sparingly)

```bash
git commit --no-verify   # skip pre-commit
git push --no-verify     # skip pre-push
```
