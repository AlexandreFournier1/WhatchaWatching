# CLAUDE.md

Guidance for Claude Code when working in this repository.

## Project

WhatchaWatching is a Blazor Web App (.NET 10, `net10.0`). It retrieves anime from **Jikan** (and movies/TV series from **TMDB**), lets users build a watchlist stored in MySQL, and offers a random picker with filters to choose what to watch next. See [README.md](README.md) for the overview and [docs/DATA_MODEL.md](docs/DATA_MODEL.md) for the data model.

Status: the app works end to end in code (adult anime are hidden unless the user turns `ShowAdultContent` on in the account page, and a switch next to the search and the random pick can hide them again for the session; anime also carry their streaming platforms, stored in `StreamingLinks`; a user can add an anime by hand, private to them, with a negative id and `Media.CreatedByUserId` set, through `ICustomAnimeService`): anime providers (Middleware: Jikan, with AniList as a fallback), services (`IMediaService`, `IWatchListService`, `IRandomPickerService`, anime cached in the database for 7 days; they take a `MediaType` because provider ids repeat from one type to another, anime come from Jikan (then AniList), movies and TV series from TMDB through `ITmdbClient`, and `Manga` throws `NotSupportedException`), and the UI (Home, My List, Search, Media detail at `/anime/{id}`, `/movie/{id}` and `/tv/{id}`, Login/Register/Account; the type switch (`MediaTypeTabs`) lives in the header and follows the address: `?type=anime|movie|tv`, or the first word of a title page, anime by default; the header links and its search keep the type, and Search, My List and Home show one type at a time) wired to them with ASP.NET Core Identity accounts. The watchlist has no status, rating or notes: it only holds what the user still wants to watch. A title is identified by its type and its provider id in the services (the Jikan id for an anime). Accounts can confirm their email, reset a forgotten password (`/forgot-password`, `/reset-password`, `/confirm-email`), change it, and delete themselves with their data (`/account`); `/privacy` says what is kept. Check the code (and the "Implementation progress" section of the data model doc) before assuming something exists.

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
| `WhatchaWatching.Middleware` | HTTP clients for Jikan and TMDB |
| `WhatchaWatching.Common` | DTOs, constants, enums |
| `WhatchaWatching.AppHost` | .NET Aspire orchestrator |
| `WhatchaWatching.Tests` | xUnit tests |

- Each layer exposes an `Add<Layer>()` extension in its `DependencyInjection.cs`. `AddApplication(IConfiguration)` calls `AddInfrastructure(...)` and `AddMiddleware()`. `Program.cs` calls only `AddApplication(...)`.
- Every service has an interface (`IXxxService`) and the UI depends on the interface only.
- Blazor components never touch `DbContext` or HTTP clients directly: they call Application services.
- Dependency injection registrations for a layer go in that layer's `DependencyInjection.cs`, not in `Program.cs`.

### Web app

- **Render mode**: only `AddInteractiveServerComponents()` is registered (Blazor Server, SignalR circuit, no WebAssembly project). `App.razor` renders every page interactively (with prerendering) except pages marked `@attribute [ExcludeFromInteractiveRouting]` (Login, Register, Account, Error): those are rendered by the server only, because they set or read the auth cookie, which a circuit cannot do. Components shared by both kinds (layout, navbar) must not need interactivity: the navbar uses plain links, a GET form, `<details>` and a checkbox. Pages load data only when `RendererInfo.IsInteractive`, so the prerender shows a skeleton instead of calling services twice.
- **Accounts**: Identity (cookies) is set up in `Program.cs`, with `UserEntity` from Infrastructure (the one place the web project uses an Infrastructure type). `ICurrentUser` (Application) gives the signed-in user id; `[Authorize]` pages redirect anonymous visitors to `/login?returnUrl=...`; sign-out is a POST to `/logout`. Return urls go through `LocalUrl.Normalize` (open-redirect protection).
- **Language**: the interface is in English or French. The English text is the key (`L["..."]` with `ILocalizer`, `Loc.T` in static helpers), `Services/Localization/FrenchTranslations.cs` holds the French; a text without a translation shows in English, and `LocalizationTests` fails if a text the UI writes has none. The language is the cookie set by `GET /language?culture=fr&returnUrl=...` (the footer links), else the `Accept-Language` of the browser, else English (`UseRequestLocalization`; the circuit takes it from the same request). Validation messages, Identity errors and the account emails go through the same localizer. Only the interface is translated: titles, synopses and genre names come from the providers in English. The TMDB attribution sentence stays in English on purpose, as TMDB requires its wording.
- **Security**: `Program.cs` reads the headers of the proxy first (`ProxyHeaders`: AlwaysData ends TLS, so without them the app takes every visit for http), then sets the security headers and the content security policy (`SecurityHeaders`, set when the response starts because the antiforgery token writes its own), then the rate limits (`RateLimiting`: 10 posts in 5 minutes to log in, sign-up, reset and account pages, 600 other requests a minute, by address). The antiforgery and sign-in cookies follow the scheme of the request. A circuit rechecks the security stamp every 10 minutes (`IdentityRevalidatingAuthenticationStateProvider`), so a changed password or a deleted account ends open sessions. Account emails (`Services/Email`: `IAccountEmailService`, `IEmailTransport`) never say whether an address has an account. Operational steps are in [docs/OPERATIONS.md](docs/OPERATIONS.md).
- **Component tree**: `Components/App.razor` → `Routes.razor` (Router, `NotFoundPage` = `Pages.NotFound`) → `Layout/MainLayout.razor` → pages in `Components/Pages/`. Shared `@using`s go in `Components/_Imports.razor`.
- **Errors**: outside Development, `/Error` + HSTS. `UseStatusCodePagesWithReExecute("/not-found")` backs the `NotFound` page.
- **Styling**: no CSS framework. Design tokens (palette, radii, fonts) are CSS variables in `wwwroot/app.css`, which also holds the shared classes (`.btn`, `.input`, `.card`, `.anime-grid`...). Components use CSS isolation (`*.razor.css`). `ReconnectModal` has a collocated `.razor.js`.
- **Static assets**: `app.MapStaticAssets()`, not `UseStaticFiles`.
- **Site icon**: `wwwroot/favicon.svg` (a pink die on the dark tile of the site, its middle pip an amber play button: the random pick of what to watch; the source of the other files), `favicon.png` (48 px, fallback) and `apple-touch-icon.png` (180 px, opaque, for home screens), declared in `App.razor`. The PNGs are drawings of the SVG: redo them when the SVG changes.
- **Version**: `<Version>` in `WhatchaWatching/WhatchaWatching.csproj` (raised by the `/commit-push` skill from what the commit changes: `feat` is a minor bump, `fix` and the like a patch, docs/tests/tooling alone none; it can also be raised by hand). The footer shows it with the short commit the SDK adds to the informational version at build time (`AppVersion`), so a deployment can be told apart from the previous one. The commit is the one checked out when building: build from a clean, committed tree.
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
- Connection string key: `ConnectionStrings:DefaultConnection`. Optional `Database:ServerVersion` (for example `8.0.36-mysql`) skips the version detection that connects to the database each time options are built until it succeeds once; without it a database outage breaks every page that uses a service. Locally it comes from Aspire (injected automatically) or from user-secrets (the web and Infrastructure projects share one `UserSecretsId`) or from the `ConnectionStrings__DefaultConnection` environment variable.
- TMDB: `Tmdb:AccessToken` is the "API Read Access Token" (sent as a Bearer token; user-secrets or the `Tmdb__AccessToken` environment variable). Without it the site still starts and the movie/TV calls report TMDB as unavailable. The country whose streaming platforms are listed is a setting of each user (`Users.Country`, default `FR`, from `CountryCatalog`), not of the site.
- Email: the `Email` section (`Host`, `Port` 587, `UserName`, `Password`, `From`, `FromName`) is the mailbox that sends the confirmation and password reset links, over STARTTLS. Without it the site still starts and nothing is sent (see [docs/OPERATIONS.md](docs/OPERATIONS.md)). Never log a link: it is a secret for two hours.
- Set a secret with `dotnet user-secrets set "<Key>" "<value>" --project WhatchaWatching`. Do not print or read stored secrets; ask the user instead.
- Do not read or edit `appsettings.Production*.json`, `.env` files or user-secrets storage.

## External APIs

### Jikan and AniList (anime)

- Use Jikan for **all anime data**. Use the MyAnimeList id as the external identifier everywhere. AniList (GraphQL, no key) is the fallback: `FallbackAnimeProvider` asks Jikan first, then AniList when Jikan fails (not when it says "not found"), and skips Jikan for 60 seconds after a failure (`ProviderCooldown`, a singleton). AniList anime are matched through their `idMal`; AniList genres have no id and are translated by name to the MyAnimeList ids (`AniListMapper`). Services depend on `IAnimeProvider`, never on a single client.
- When no provider answers, `IMediaService.SearchAsync` searches the anime already stored in the database (`IsLocalOnly`) and the Search page says so; it throws `MediaProviderUnavailableException` only if nothing stored matches.
- Timeouts are short on purpose (5 s per attempt, 12 s total, one retry): a person is waiting.
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
- **Secret scan** ([gitleaks](https://github.com/gitleaks/gitleaks), rules in `.gitleaks.toml`): the git hooks in `.githooks/` (`pre-commit` scans the staged files and refuses `appsettings.Production*.json`, `.env*` and `secrets.json`; `pre-push` scans the commits to push) and a Claude Code `PreToolUse` hook (`.claude/hooks/check-secrets.sh`, set in `.claude/settings.json`) that runs the same scan before any `git commit` or `git push` command. All use `.githooks/scan-secrets.sh`. Never bypass them (`--no-verify`): if one blocks, remove the secret, do not allowlist it. Setup on a new clone is in [docs/OPERATIONS.md](docs/OPERATIONS.md).
