---
name: commit-push
description: Commit and push the current changes to the GitHub remote with a detailed commit message, raising the version of the site first when the changes call for it, and asking for confirmation before the commit and before the push. Use when the user wants to commit, push, save or ship their current work.
disable-model-invocation: true
allowed-tools: Bash(git status *) Bash(git diff *) Bash(git log *) Bash(git branch *) Bash(git remote *) Bash(git rev-parse *) Bash(git show *) Bash(bash .claude/skills/commit-push/bump-version.sh *)
---

# Commit and push

Commit the current working-tree changes with a detailed message, then push them to the remote.
Two confirmations are mandatory: one before creating the commit, one before pushing.
Committing and pushing are hard to undo once shared, so the user must see exactly what will happen first.

## Current state

- Branch: !`git branch --show-current 2>&1 || true`
- Remote: !`git remote -v 2>&1 || true`
- Status: !`git status --short 2>&1 || true`
- Recent commits (for message style): !`git log --oneline -5 2>&1 || true`
- Version of the site: !`grep -o '<Version>[^<]*</Version>' WhatchaWatching/WhatchaWatching.csproj 2>&1 || true`

## Steps

### 1. Understand the changes
- If the status above is empty or this is not a git repository, tell the user and stop.
- Run `git diff HEAD` to read tracked changes. For untracked files, read them to understand what they contain.
- Work out what was done and why, not just which lines changed.

### 2. Safety checks
Before proposing anything, look for problems and raise them to the user:
- Files that look like secrets (`.env`, keys, certificates, connection strings, tokens, `appsettings.*.json` with credentials). Exclude them from staging and warn the user.
- Build output or large binaries (`bin/`, `obj/`, `node_modules/`, archives) that should not be committed.
- Being on `main` or `master` directly: mention it and let the user decide.
- Clearly unrelated changes mixed together: suggest splitting into several commits.

### 3. Draft the commit message
Follow the language and style of the recent commits shown above. If there is no clear style, use Conventional Commits.

- **Subject**: `type(scope): summary`, imperative mood, 72 characters max (types: feat, fix, refactor, docs, test, chore, ci, build, perf).
- **Body**: detailed, wrapped at about 72 characters, covering:
  - What changed, grouped by area or component (short bullet list)
  - Why it changed (the motivation or problem solved)
  - Notable decisions, trade-offs, or follow-ups
  - `BREAKING CHANGE:` footer if applicable
- Be specific ("add retry policy to the payment client") rather than vague ("update code").
- Only describe what is actually in the diff. Never invent changes.

### 4. Decide the version of the site
The footer of the site shows its version (`<Version>` in `WhatchaWatching/WhatchaWatching.csproj`, for example `v0.1.0 (cdade22)`: the version, then the commit it was built from). This step raises the version so that the user does not have to, by reading what the commit(s) change. Decide now, but do **not** edit the file before the user confirms (step 5).

**No bump** when any of these is true:
- The `<Version>` line already differs from `HEAD` (`git diff HEAD -- WhatchaWatching/WhatchaWatching.csproj`): the user raised it by hand, keep their number.
- Every changed file is outside the shipped site: Markdown files, `docs/`, `WhatchaWatching.Tests/`, `.claude/`, `.github/`, `.gitignore`. Docs, tests and tooling alone do not make a new version.
- The user's arguments say not to bump ("no bump", "skip version").

**Otherwise choose the level** from the commit(s) being created (several commits in one run: one bump for the run, the highest level wins):

| What the commit does | Level |
|---|---|
| `BREAKING CHANGE` footer or `!` after the type | `major`, but `minor` while the version starts with `0.` (not stable yet) |
| `feat` | `minor` |
| `fix`, `perf`, `refactor`, `style`, `build`, or `chore` / `ci` that changes shipped files | `patch` |

If the arguments name a level (`major`, `minor`, `patch`), use that instead.

Compute the result without touching the file:
`bash .claude/skills/commit-push/bump-version.sh <level> --dry-run` prints `0.1.0 -> 0.2.0`.
If the script fails (for example the version is a pre-release such as `1.0.0-beta`), tell the user and continue without a bump.

The bump belongs to the commit that justifies it (the one with the highest level; the only one when there is a single commit), so that the version and the change that needs it stay together.

### 5. Confirmation 1: before the commit
Use AskUserQuestion (or plain text if unavailable) and show:
- The branch name
- The exact list of files that will be staged, **including `WhatchaWatching/WhatchaWatching.csproj` when the version is raised**
- The version: `0.1.0 -> 0.2.0 (minor, because feat: ...)`, or "version unchanged" with the reason
- The full commit message(s)

Options: **Commit**, **Edit the message**, **Cancel**.
Do not edit the version, stage or commit anything until the user picks Commit. If they want edits, apply them, show the result again, and ask again. If the edit changes the type of the commit (for example `feat` becomes `fix`), decide the bump again.

### 6. Create the commit
- If the version is raised, run `bash .claude/skills/commit-push/bump-version.sh <level>` now (without `--dry-run`) and check that it printed the number announced in the confirmation.
- Stage only the confirmed files by name (`git add <files>`). Avoid `git add -A`, which could pick up files that were excluded.
- Commit with the confirmed message exactly as approved (use a heredoc or `-F`, so line breaks are preserved).
- Never use `--no-verify`, never amend an existing commit unless the user asks.
- If a pre-commit hook fails, report the output, fix the problem if it is simple and in scope, re-stage, and make a new commit. Otherwise stop and explain.
- Show the resulting short hash and subject.

### 7. Confirmation 2: before the push
Show the user:
- The remote name and URL, and the target branch
- The commits that will be pushed (`git log --oneline @{u}..HEAD`, or all of `HEAD` if no upstream exists)

Ask: **Push**, or **Don't push**. Do not push until the user picks Push.
If the user declines, stop. The commit stays local and that is a valid outcome.

### 8. Push
- Run `git push`. If the branch has no upstream, run `git push -u origin <branch>`.
- Never force push (`--force`, `--force-with-lease`) unless the user explicitly asks for it in this conversation.
- If the push is rejected (remote has new commits), explain, propose `git pull --rebase`, and ask before doing anything else.

### 9. Report
End with a short summary: commit hash, branch, remote, the version of the site (raised or not), and whether the push succeeded.
Remind the user that the site shows the new version only after `/deploy-alwaysdata`.

## Rules
- Both confirmations are required every time, even if the user's arguments say "yes" or "just do it". The arguments describe the work, they do not replace the check.
- Never commit or push files that look like secrets.
- Never rewrite history that has already been pushed.
- The version is raised at most once per run, never when the user already changed it, and never without being shown in the confirmation.
