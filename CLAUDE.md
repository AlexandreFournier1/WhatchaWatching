# CLAUDE.md

Guidance for Claude Code when working in this repository. Rules specific to a project live in a `CLAUDE.md` inside its folder and load when you work there: `WhatchaWatching/` (web app, accounts, language, security), `WhatchaWatching.Application/`, `WhatchaWatching.Infrastructure/` (database, migrations), `WhatchaWatching.Middleware/` (Jikan, AniList, TMDB), `WhatchaWatching.Tests/`, `WhatchaWatching.AppHost/`.

## Project

WhatchaWatching is a Blazor Web App (.NET 10, `net10.0`). It retrieves anime from **Jikan** (and movies/TV series from **TMDB**), lets users build a watchlist stored in MySQL, and offers a random picker with filters to choose what to watch next. See [README.md](README.md) for the overview and [docs/DATA_MODEL.md](docs/DATA_MODEL.md) for the data model.

Status: the app works end to end in code: anime, movies and TV series (one type at a time, switched in the header and followed by the address: `?type=anime|movie|tv`), watchlist, random picker, search, title pages, ASP.NET Core Identity accounts (email confirmation, password reset and change, self-deletion), English/French interface, anime added by hand (private to their author), adult anime hidden by default. Check the code (and the "Implementation progress" section of the data model doc) before assuming something exists.

Solution file: `WhatchaWatching.slnx` (new XML format).

## Commands

Run from the repo root.

- Build: `dotnet build`
- Tests: `dotnet test` (must pass before every commit)
- Restore local tools (`dotnet-ef`): `dotnet tool restore`
- Run with Aspire (MySQL container included): `dotnet run --project WhatchaWatching.AppHost --launch-profile http` (dashboard at http://localhost:15209)
- Run the web app alone: `dotnet run --project WhatchaWatching --launch-profile http` (http://localhost:5209; needs a configured connection string)
- Hot reload: `dotnet watch --project WhatchaWatching`

`bin/`, `obj/` and `publish/` are build artifacts: ignore them.

## Architecture

Clean Architecture. Dependencies flow one way: **Web → Application → (Infrastructure, Middleware) → Common**. Never reference a layer to its left from a layer to its right.

| Project | Contains |
| --- | --- |
| `WhatchaWatching` | Blazor UI, `Program.cs` |
| `WhatchaWatching.Application` | Services (with interfaces) and mappers |
| `WhatchaWatching.Infrastructure` | EF Core `ApplicationDbContext`, entities, configurations, migrations |
| `WhatchaWatching.Middleware` | HTTP clients for Jikan, AniList and TMDB |
| `WhatchaWatching.Common` | DTOs, constants, enums |
| `WhatchaWatching.AppHost` | .NET Aspire orchestrator |
| `WhatchaWatching.Tests` | xUnit tests |

- Each layer exposes an `Add<Layer>()` extension in its `DependencyInjection.cs`. `AddApplication(IConfiguration)` calls `AddInfrastructure(...)` and `AddMiddleware()`. `Program.cs` calls only `AddApplication(...)`; registrations for a layer go in that layer's file.
- Every service has an interface (`IXxxService`) and the UI depends on the interface only.
- Blazor components never touch `DbContext` or HTTP clients directly: they call Application services.
- Nullable reference types and implicit usings are enabled everywhere.

## Code conventions

Match the surrounding code. Observed conventions:

- **Naming**: entities `XxxEntity`, EF configurations `XxxEntityConfiguration : IEntityTypeConfiguration<XxxEntity>`, services `XxxService` / `IXxxService`, DTOs `XxxDto`. Constants are `UPPER_SNAKE_CASE` in `Common/Constants`; enums live in `Common/Enum`.
- **File-scoped namespaces**, one type per file, namespace equal to the folder path.
- **Async everywhere** for I/O: `Task`-returning methods, `CancellationToken` parameter on service and client methods, `await` EF Core async APIs.
- **No magic numbers or strings** for lengths and names: use `ConfigurationConstants` (column sizes) and `DatabaseConstants`.
- **Comments** only to explain a non-obvious *why* (see the existing ones in `DependencyInjection.cs` and the configurations). Do not narrate what the code does.
- Do not add features, abstractions or error handling beyond what the task needs.
- New UI text goes through the localizer; run `/add-translation` for the French.

## Configuration and secrets

- Never hard-code credentials, connection strings or API keys, and never commit them.
- Connection string key: `ConnectionStrings:DefaultConnection`. Optional `Database:ServerVersion` (for example `8.0.36-mysql`) skips the version detection that connects to the database each time options are built until it succeeds once; without it a database outage breaks every page that uses a service. Locally it comes from Aspire (injected automatically) or from user-secrets (the web and Infrastructure projects share one `UserSecretsId`) or from the `ConnectionStrings__DefaultConnection` environment variable.
- TMDB: `Tmdb:AccessToken` (user-secrets or the `Tmdb__AccessToken` environment variable); details in the Middleware `CLAUDE.md`.
- Email: the `Email` section (`Host`, `Port` 587, `UserName`, `Password`, `From`, `FromName`) is the mailbox that sends the confirmation and password reset links, over STARTTLS. Without it the site still starts and nothing is sent (see [docs/OPERATIONS.md](docs/OPERATIONS.md)). Never log a link: it is a secret for two hours.
- Set a secret with `dotnet user-secrets set "<Key>" "<value>" --project WhatchaWatching`. Do not print or read stored secrets; ask the user instead.
- Do not read or edit `appsettings.Production*.json`, `.env` files or user-secrets storage (a hook and deny rules enforce it).

## Testing

`dotnet test` after any code change and before every commit; report failures with their output, never claim success without running them. Style and rules are in `WhatchaWatching.Tests/CLAUDE.md`.

## Claude tooling (`.claude/`)

- **Hooks** (`settings.json`): `check-secrets.sh` scans for secrets before `git commit` / `git push`; `guard-files.sh` blocks edits to generated EF files (`*ModelSnapshot.cs`, `*.Designer.cs`) and reads of secret files; `build-after-edit.sh` runs `dotnet build` after a `.cs` or `.razor` edit and returns compiler errors.
- **Permissions**: build, test and read-only git commands are allowed; `dotnet ef database update`, force pushes, `--no-verify` and `dotnet user-secrets list` are denied.
- **Skills**: `/commit-push`, `/new-migration` (creates a migration, never applies it), `/apply-migrations`, `/deploy-alwaysdata`, `/add-translation`, `/smoke-test` (walks the main journeys in the browser on a local run).
- **CI**: `.github/workflows/ci.yml` builds, tests, checks for vulnerable packages and scans for secrets on every push to `main` and every pull request.
- **Agents**: `architecture-reviewer` (layers, data access, DI, external calls) and `security-reviewer` (accounts, redirects, ownership, headers, secrets): use them before committing changes in those areas. Their report formats are in `.claude/templates/`.

## Git

- Commit and push through the `/commit-push` skill (asks for confirmation before each step).
- `main` is the default branch. Do not commit generated build output or secrets.
- **Secret scan** ([gitleaks](https://github.com/gitleaks/gitleaks), rules in `.gitleaks.toml`): the git hooks in `.githooks/` (`pre-commit` scans the staged files and refuses `appsettings.Production*.json`, `.env*` and `secrets.json`; `pre-push` scans the commits to push) and the Claude Code `PreToolUse` hook above both use `.githooks/scan-secrets.sh`. Never bypass them (`--no-verify`): if one blocks, remove the secret, do not allowlist it. Setup on a new clone is in [docs/OPERATIONS.md](docs/OPERATIONS.md).
