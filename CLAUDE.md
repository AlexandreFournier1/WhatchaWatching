# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

WhatchaWatching is a Blazor Web App (.NET 10, `net10.0`). It retrieves anime from **Jikan** (and movies/TV series from **TMDB**), lets users build a watchlist stored in MySQL, and offers a random picker with filters to choose what to watch next. See [README.md](README.md) for the overview and [docs/DATA_MODEL.md](docs/DATA_MODEL.md) for the data model.

Status: the UI is built (Home, My List, Search, Anime detail, Login/Register/Account) but runs on placeholder data from `WhatchaWatching/Components/Preview/PreviewWatchList.cs`, to be replaced by real services. The watchlist has no status, rating or notes: it only holds what the user still wants to watch. Services, Jikan/TMDB clients and the Identity setup in the web app are not implemented yet. Check the code (and the "Implementation progress" section of the data model doc) before assuming something exists.

Solution file: `WhatchaWatching.slnx` (new XML format).

## Commands

Run from the repo root.

- Build: `dotnet build`
- Tests: `dotnet test` (must pass before every commit)
- Restore local tools (`dotnet-ef`): `dotnet tool restore`
- Run with Aspire (MySQL container included): `dotnet run --project WhatchaWatching.AppHost --launch-profile http` (dashboard at http://localhost:17069)
- Run the web app alone: `dotnet run --project WhatchaWatching --launch-profile http` (needs a configured connection string)
- Hot reload: `dotnet watch --project WhatchaWatching`

`bin/`, `obj/` and `publish/` are build artifacts: ignore them.

## Architecture

Clean Architecture. Dependencies flow one way: **Web → Application → (Infrastructure, Middleware) → Common**. Never reference a layer to its left from a layer to its right.

| Project | Contains |
| --- | --- |
| `WhatchaWatching` | Blazor UI, `Program.cs` |
| `WhatchaWatching.Application` | Services (with interfaces) and mappers |
| `WhatchaWatching.Infrastructure` | EF Core `ApplicationDbContext`, entities, configurations, migrations |
| `WhatchaWatching.Middleware` | HTTP clients for Jikan and TMDB |
| `WhatchaWatching.Common` | DTOs, constants, enums |
| `WhatchaWatching.AppHost` | .NET Aspire orchestrator |
| `WhatchaWatching.Tests` | xUnit tests |

- Each layer exposes an `Add<Layer>()` extension in its `DependencyInjection.cs`. `AddApplication(IConfiguration)` calls `AddInfrastructure(...)` and `AddMiddleware()`. `Program.cs` calls only `AddApplication(...)`.
- Every service has an interface (`IXxxService`) and the UI depends on the interface only.
- Blazor components never touch `DbContext` or HTTP clients directly: they call Application services.
- Dependency injection registrations for a layer go in that layer's `DependencyInjection.cs`, not in `Program.cs`.

### Web app

- **Render mode**: only `AddInteractiveServerComponents()` is registered (Blazor Server, SignalR circuit, no WebAssembly project). A page is interactive only if it sets `@rendermode InteractiveServer` (or it is set in `App.razor`). Check before assuming a component is interactive.
- **Component tree**: `Components/App.razor` → `Routes.razor` (Router, `NotFoundPage` = `Pages.NotFound`) → `Layout/MainLayout.razor` → pages in `Components/Pages/`. Shared `@using`s go in `Components/_Imports.razor`.
- **Errors**: outside Development, `/Error` + HSTS. `UseStatusCodePagesWithReExecute("/not-found")` backs the `NotFound` page.
- **Styling**: no CSS framework. Design tokens (palette, radii, fonts) are CSS variables in `wwwroot/app.css`, which also holds the shared classes (`.btn`, `.input`, `.card`, `.anime-grid`...). Components use CSS isolation (`*.razor.css`). `ReconnectModal` has a collocated `.razor.js`.
- **Static assets**: `app.MapStaticAssets()`, not `UseStaticFiles`.
- Nullable reference types and implicit usings are enabled everywhere.

### Aspire AppHost

[Program.cs](WhatchaWatching.AppHost/Program.cs) is the single place that declares resources (MySQL container + `DefaultConnection` database). Add new resources there and chain `.WithReference(...)` to the web app. The resource name must stay equal to `DatabaseConstants.CONNECTION_NAME`. The `Aspire.AppHost.Sdk` SDK needs no separate Aspire package reference; `Projects.WhatchaWatching` is source-generated.

## Code conventions

Match the surrounding code. Observed conventions:

- **Naming**: entities `XxxEntity`, EF configurations `XxxEntityConfiguration : IEntityTypeConfiguration<XxxEntity>`, services `XxxService` / `IXxxService`, DTOs `XxxDto`. Constants are `UPPER_SNAKE_CASE` in `Common/Constants`; enums live in `Common/Enum`.
- **File-scoped namespaces**, one type per file, namespace equal to the folder path.
- **Async everywhere** for I/O: `Task`-returning methods, `CancellationToken` parameter on service and client methods, `await` EF Core async APIs.
- **No magic numbers or strings** for lengths and names: use `ConfigurationConstants` (column sizes) and `DatabaseConstants`.
- **Comments** only to explain a non-obvious *why* (see the existing ones in `DependencyInjection.cs` and the configurations). Do not narrate what the code does.
- Do not add features, abstractions or error handling beyond what the task needs.

## Database

MySQL through **EF Core** with the **Pomelo.EntityFrameworkCore.MySql** provider. Production runs on **AlwaysData** (MySQL).

- No SQL Server, PostgreSQL or SQLite specifics unless explicitly requested.
- Schema changes go through **EF Core migrations**, never by editing the database by hand.
- Data access lives in Application services (via `IDbContextFactory<ApplicationDbContext>`), never in Blazor components. Blazor Server needs short-lived contexts: create one per operation from the factory.
- Query only what is needed (`Select` projections, `AsNoTracking` for reads). Add indexes, foreign keys and constraints deliberately, as in the existing configurations.
- Entities that implement `IAuditableEntity` get their timestamps set by `ApplicationDbContext`; do not set them manually.

### Migrations workflow

Entities are in `Infrastructure/Data/Entities`, configurations in `Infrastructure/Data/Configurations`, migrations in `Infrastructure/Migrations`. The design-time factory (`ApplicationDbContextFactory`) lets the EF tools run from the Infrastructure project alone, and it needs no live database.

1. Change the entity and its configuration.
2. Create the migration:
   `dotnet ef migrations add <PascalCaseName> --project WhatchaWatching.Infrastructure`
3. Read the generated migration to confirm it matches the intent (no unexpected drops).
4. Update [docs/DATA_MODEL.md](docs/DATA_MODEL.md) so it matches the model.
5. Run `dotnet build` and `dotnet test`.

Rules:
- Never hand-edit `ApplicationDbContextModelSnapshot.cs` or `*.Designer.cs`: they are generated.
- Never edit a migration that was already applied or pushed: add a new one.
- To discard a migration that was never applied, use `dotnet ef migrations remove --project WhatchaWatching.Infrastructure` (do not delete files by hand).
- Do not run `dotnet ef database update` against the production database. Applying migrations to production is the user's decision.

## Configuration and secrets

- Never hard-code credentials, connection strings or API keys, and never commit them.
- Connection string key: `ConnectionStrings:DefaultConnection`. Locally it comes from Aspire (injected automatically) or from user-secrets (the web and Infrastructure projects share one `UserSecretsId`) or from the `ConnectionStrings__DefaultConnection` environment variable.
- Set a secret with `dotnet user-secrets set "<Key>" "<value>" --project WhatchaWatching`. Do not print or read stored secrets; ask the user instead.
- Do not read or edit `appsettings.Production*.json`, `.env` files or user-secrets storage.

## External APIs

### Jikan (anime)

- Use Jikan for **all anime data**. Use the MyAnimeList/Jikan ID as the external identifier.
- Keep calls in dedicated clients in `WhatchaWatching.Middleware`, behind an interface.

### TMDB (movies and TV series)

- Use TMDB for movies and TV series only, never for anime that Jikan covers. Use the TMDB ID as the external identifier.
- Keep calls in dedicated clients in `WhatchaWatching.Middleware`. Store the API key in configuration (user-secrets / environment variable), never in source.
- Data available: titles, original titles, overview, release dates, genres, ratings, posters, backdrops, cast and crew, videos, watch providers; search by title through the search endpoints.

### Rules for both

- Register clients with `IHttpClientFactory` (typed clients) and configure resilience (timeouts, retry, rate-limit handling, e.g. `Microsoft.Extensions.Http.Resilience`).
- Any field can be missing or `null` in a response: model it as nullable and handle it.
- Handle API errors, timeouts and rate limits gracefully; never let an external failure crash a page.
- Avoid redundant calls: reuse data already stored in the local database (`Media.LastSyncedAt`) when appropriate.
- Never call an external API from a Blazor component.

## Testing

- xUnit (`WhatchaWatching.Tests`), `Xunit` is a global using. Follow the existing style in `DependencyInjectionTests.cs`: one behavior per `[Fact]`, names like `Method_Condition_Expected`.
- Test Application services and mappers. Mock HTTP with a fake `HttpMessageHandler`; do not call real Jikan/TMDB endpoints from tests.
- Run `dotnet test` after any code change and before every commit. Report failures with their output; never claim success without running them.

## Git

- Commit and push through the `/commit-push` skill (asks for confirmation before each step).
- `main` is the default branch. Do not commit generated build output or secrets.
