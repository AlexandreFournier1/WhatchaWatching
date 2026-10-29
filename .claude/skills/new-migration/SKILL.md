---
name: new-migration
description: Create an EF Core migration for the entity or configuration changes in the working tree, check what it does, update docs/DATA_MODEL.md, then build and test. Use when the user changed the data model and needs a migration. It never applies anything to a database (that is /apply-migrations).
argument-hint: <PascalCaseName>
disable-model-invocation: true
allowed-tools: Bash(dotnet tool restore) Bash(dotnet ef migrations add *) Bash(dotnet ef migrations list *) Bash(dotnet ef migrations remove *) Bash(dotnet build*) Bash(dotnet test*) Bash(git status *) Bash(git diff *)
---

# Create a migration

Follows the workflow in `WhatchaWatching.Infrastructure/CLAUDE.md`. It only creates files; the database is not touched.

## Current state
- Changes: !`git status --short 2>&1 || true`

## Steps

### 1. Check there is something to migrate
- Read `git diff HEAD` for `WhatchaWatching.Infrastructure/Data/` (entities, configurations, `ApplicationDbContext`). If nothing there changed, say so and stop.
- If the working tree already holds an unapplied migration that was never pushed for this same change, ask the user whether to remove it first (`dotnet ef migrations remove --project WhatchaWatching.Infrastructure`) instead of stacking a second one.

### 2. Name it
- Use the argument if given. Otherwise propose a PascalCase name describing the change (`AddUserCountry`, not `Update1`) and use it.
- The name must not already exist in `WhatchaWatching.Infrastructure/Migrations/`.

### 3. Create it
- `dotnet tool restore`, then `dotnet ef migrations add <Name> --project WhatchaWatching.Infrastructure`.
- If it fails, show the error and fix the model or configuration, not the generated files. Never hand-edit `ApplicationDbContextModelSnapshot.cs` or `*.Designer.cs`.

### 4. Read what it generates
Read the new migration's `Up` and `Down` and check against the intent:
- Unexpected `DropTable`, `DropColumn`, `DropIndex` or `DropForeignKey`, renames that EF turned into drop + add, columns that became `NOT NULL` without a default, narrowed types or lengths.
- Column sizes come from `ConfigurationConstants`, indexes and foreign keys are the deliberate ones.
- Anything destructive: say which data it would lose. If it is not intended, fix the entity or configuration, run `migrations remove`, and create it again.

### 5. Update the docs
Update `docs/DATA_MODEL.md` so it matches the model (tables, columns, indexes, relations), and add the migration to its "Database state" section as **Not applied yet**.

### 6. Verify
Run `dotnet build` then `dotnet test`. Report failures with their output.

### 7. Report
Name the migration created, what it does in plain language, any destructive operation, and that nothing was applied: `/apply-migrations` does that, after a backup. Leave the files uncommitted (`/commit-push` commits them).
