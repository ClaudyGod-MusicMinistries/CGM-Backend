# Git hooks

Native git hooks — no Husky, no npm/NuGet package. Tracked in version control here instead of the untracked `.git/hooks/`, and wired up via git's built-in `core.hooksPath` setting.

## One-time setup (per clone)

```bash
git config core.hooksPath .githooks
```

That's it — every commit and push from then on runs the hooks below automatically.

## What runs

- **pre-commit** — `dotnet format --verify-no-changes` on staged `.cs` files only. Fast; fails and tells you what to fix rather than silently rewriting your changes.
- **pre-push** — full `dotnet build` (Release) + full `dotnet test` across every project, mirroring the exact steps `.github/workflows/build-push.yml` runs in CI. If this fails locally, it would have failed CI too.

## Bypassing (use sparingly)

```bash
git commit --no-verify   # skip pre-commit
git push --no-verify     # skip pre-push
```
