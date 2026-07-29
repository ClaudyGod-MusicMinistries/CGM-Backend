# Git hooks

Native Git hooks with a polished terminal quality-gate UI. There is no Husky, Lefthook, npm hook dependency, or NuGet hook dependency. Hooks are tracked here and activated through Git's built-in `core.hooksPath`.

Each named check shows its result and elapsed time. Failures stop the Git operation, retain the command's real output, and provide an actionable recovery message. Set `NO_COLOR=1` for plain CI/accessibility output.

## One-time setup (per clone)

```bash
./scripts/install-git-hooks.sh
```

That's it — every commit and push from then on runs the hooks below automatically.

## What runs

- **pre-commit** — staged whitespace/conflict-marker and credential checks, followed by C# formatting verification. It never rewrites staged work.
- **pre-push** — restore, formatting, vulnerability audit, Release build, EF migration-drift check, unit/API tests, and disposable PostgreSQL integration tests when Docker is available. GitHub Actions always runs the PostgreSQL suite. Set `REQUIRE_INTEGRATION_TESTS=1` to make Docker mandatory locally as well.

## Bypassing (use sparingly)

```bash
git commit --no-verify   # skip pre-commit
git push --no-verify     # skip pre-push
```
