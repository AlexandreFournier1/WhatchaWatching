---
name: apply-migrations
description: Apply the pending EF Core migrations to the production MySQL database hosted on AlwaysData, after previewing the SQL and asking for confirmation. Use when the user wants to apply, run, deploy or push migrations to the database.
disable-model-invocation: true
allowed-tools: Bash(dotnet tool restore) Bash(dotnet ef migrations list *) Bash(dotnet ef migrations script *) Bash(git status *) Bash(git log *)
---

# Apply migrations to the AlwaysData database

Apply the pending EF Core migrations of `WhatchaWatching.Infrastructure` to the production database.
This changes a live database and some migrations drop columns or tables (data is lost), so the user must see
exactly what will run and confirm before anything is applied.

## Rules
- The connection string is a secret. Never print it, never ask the user to paste it in the chat, never put it on a command line, in a file or in a commit.
  It comes from the web project's user-secrets (`ConnectionStrings:DefaultConnection`) or from the environment variable `ConnectionStrings__DefaultConnection`.
- Never run `dotnet user-secrets list` or anything else that would display the secret value.
- Never use `--connection` on `dotnet ef`, and never run `dotnet ef database drop` or `migrations remove` here.
- The confirmation below is required every time, even if the user's arguments say "yes" or "just do it".

## Current state
- Branch and changes: !`git status --short --branch 2>&1 || true`
- Latest commits: !`git log --oneline -3 2>&1 || true`

## Steps

### 1. Check the repository
- If there are uncommitted changes to `WhatchaWatching.Infrastructure/` (entities, configurations or `Migrations/`), warn the user: the database should match committed code. Let them decide whether to continue.
- Run `dotnet tool restore` (the repo-local `dotnet-ef` tool).

### 2. List applied and pending migrations
Run `dotnet ef migrations list --project WhatchaWatching.Infrastructure`.
- Migrations marked `(Pending)` are the ones that would be applied. If there are none, say so and stop.
- If the command fails to connect, the connection string is missing or wrong (without a secret the tool uses a placeholder with no credentials).
  Do not try to work around it. Tell the user to set it themselves in their own terminal, then stop:
  - `dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>" --project WhatchaWatching` (once), or
  - the environment variable `ConnectionStrings__DefaultConnection` for the current shell.
  The AlwaysData host looks like `mysql-<account>.alwaysdata.net`, and remote access to the database must be allowed in the AlwaysData admin panel.

### 3. Preview the SQL
Run `dotnet ef migrations script <last applied migration> --project WhatchaWatching.Infrastructure`
(the argument is the last migration that is not pending; omit it if nothing has been applied yet).

Read the script and summarise, per pending migration, in plain language:
- Tables, columns, indexes and constraints that are created, changed or dropped.
- **Destructive operations** (`DROP TABLE`, `DROP COLUMN`, type narrowing, `NOT NULL` added without a default): list each one and say which data would be lost.

### 4. Confirmation
Use AskUserQuestion (or plain text if unavailable) and show:
- The pending migrations, in order.
- The summary from step 3, with the destructive operations in bold.
- A reminder to make a backup first (AlwaysData admin panel: Databases > MySQL > export, or phpMyAdmin), since a failed or unwanted migration cannot be undone by this skill.

Options: **Apply**, **Cancel**. Do not apply anything until the user picks Apply and, if there are destructive operations, confirms the backup exists.

### 5. Apply
Run `dotnet ef database update --project WhatchaWatching.Infrastructure`.
- Run it once. If it fails, show the error (without any connection string), do not retry in a loop, and explain what state the database may be in (EF Core wraps each migration, but MySQL DDL is not transactional, so a migration can be partially applied).
- Never apply a different target or edit migration files to make it pass.

### 6. Verify and record
- Run `dotnet ef migrations list --project WhatchaWatching.Infrastructure` again: no migration should remain `(Pending)`.
- Update the "Database state" section of `docs/DATA_MODEL.md`: replace "**Not applied yet**" with the application date for each migration that was applied.
  Leave the file edited but uncommitted (the user commits with `/commit-push`).

### 7. Report
End with a short summary: migrations applied, whether the verification passed, and what was recorded in the docs.
