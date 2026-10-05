---
name: commit-push
description: Commit and push the current changes to the GitHub remote with a detailed commit message, asking for confirmation before the commit and before the push. Use when the user wants to commit, push, save or ship their current work.
disable-model-invocation: true
allowed-tools: Bash(git status *) Bash(git diff *) Bash(git log *) Bash(git branch *) Bash(git remote *) Bash(git rev-parse *) Bash(git show *)
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

### 4. Confirmation 1: before the commit
Use AskUserQuestion (or plain text if unavailable) and show:
- The branch name
- The exact list of files that will be staged
- The full commit message

Options: **Commit**, **Edit the message**, **Cancel**.
Do not stage or commit anything until the user picks Commit. If they want edits, apply them, show the result again, and ask again.

### 5. Create the commit
- Stage only the confirmed files by name (`git add <files>`). Avoid `git add -A`, which could pick up files that were excluded.
- Commit with the confirmed message exactly as approved (use a heredoc or `-F`, so line breaks are preserved).
- Never use `--no-verify`, never amend an existing commit unless the user asks.
- If a pre-commit hook fails, report the output, fix the problem if it is simple and in scope, re-stage, and make a new commit. Otherwise stop and explain.
- Show the resulting short hash and subject.

### 6. Confirmation 2: before the push
Show the user:
- The remote name and URL, and the target branch
- The commits that will be pushed (`git log --oneline @{u}..HEAD`, or all of `HEAD` if no upstream exists)

Ask: **Push**, or **Don't push**. Do not push until the user picks Push.
If the user declines, stop. The commit stays local and that is a valid outcome.

### 7. Push
- Run `git push`. If the branch has no upstream, run `git push -u origin <branch>`.
- Never force push (`--force`, `--force-with-lease`) unless the user explicitly asks for it in this conversation.
- If the push is rejected (remote has new commits), explain, propose `git pull --rebase`, and ask before doing anything else.

### 8. Report
End with a short summary: commit hash, branch, remote, and whether the push succeeded.

## Rules
- Both confirmations are required every time, even if the user's arguments say "yes" or "just do it". The arguments describe the work, they do not replace the check.
- Never commit or push files that look like secrets.
- Never rewrite history that has already been pushed.