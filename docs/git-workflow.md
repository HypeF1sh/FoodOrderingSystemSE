# Git Workflow

The brief checks branches, commits, pull requests and merges. A steady history of small,
well-named commits from all three members is worth 5 marks. A single bulk upload scores badly.

## Branches
| Branch | Purpose | Rule |
|---|---|---|
| `main` | Stable, demo-ready | Only updated by merging `dev` at the end of each sprint |
| `dev` | Integration | Receives feature branches through pull requests |
| `feature/PBxx-short-name` | One backlog item | Branch from `dev`, PR back into `dev` |
| `hotfix/short-name` | Urgent fix on `main` | Merge into both `main` and `dev` |

No direct commits to `main`.

## Commit messages — Conventional Commits with backlog ID
```
feat(PB06): add menu item create and edit pages
fix(PB14): correct cart total when quantity changes
refactor(PB15): move status transition check into OrderService
docs: update progress log
chore: add EF Core packages
```
Types: `feat` new feature · `fix` bug · `refactor` restructure · `style` CSS/formatting · `docs` · `test` · `chore` setup.
Commit small and often — several times per working session, not once per sprint.

## Daily loop
```powershell
git checkout dev
git pull                                   # get teammates' work
git checkout -b feature/PB06-menu-admin    # new item  (or: git checkout feature/... to resume)

# ...work...
git status
git add <files>
git commit -m "feat(PB06): add menu category list page"
git push -u origin feature/PB06-menu-admin # first push of the branch; later just: git push
```
Then on GitHub: **Compare & pull request** → base `dev` → ask a teammate to review → merge.

## Keeping a branch up to date
```powershell
git checkout dev
git pull
git checkout feature/PB06-menu-admin
git merge dev
```

## Never commit
`bin/`, `obj/`, `.vs/`, `*.user`, database files, secrets or API keys, `CLAUDE.local.md`,
`.claude/settings.local.json`. The `.gitignore` handles these — check `git status` before `git add .`.

## Migrations
Only one person adds a migration at a time. Post in Discord before pushing one.
If two migrations collide, the second person deletes theirs, pulls, and re-adds it.

## Evidence for the report (Chapter 3)
Screenshots to capture once there's real activity:
branch list · your commit history (filtered by author) · a merged PR with a review · Insights → Contributors · the GitHub Projects board.
