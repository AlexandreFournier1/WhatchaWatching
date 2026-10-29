# WhatchaWatching

WhatchaWatching is a Blazor Web App (.NET 10) to keep a list of the anime you want to watch, and to let chance pick the next one. Anime come from [Jikan](https://jikan.moe/) (MyAnimeList), with [AniList](https://anilist.co/) as a fallback when Jikan does not answer.

**Live site:** [https://whatcha-watching.alwaysdata.net](https://whatcha-watching.alwaysdata.net)

[![CI](https://github.com/AlexandreFournier1/WhatchaWatching/actions/workflows/ci.yml/badge.svg)](https://github.com/AlexandreFournier1/WhatchaWatching/actions/workflows/ci.yml)

## Features

- **Anime, movies and TV series**: one switch in the header (Anime / Movies / TV series) sets the type for the whole site: the search, the list, the random pick and the title pages (`?type=anime|movie|tv`). The links and the search of the header keep the type.
- **Search** by title: anime from Jikan, then AniList if Jikan is down; movies and series from [TMDB](https://www.themoviedb.org/). When the providers are down, the titles already saved on the site are searched, with a notice.
- **Title page** (`/anime/{id}`, `/movie/{id}`, `/tv/{id}`): poster, summary, genres, episodes and seasons, duration, status, score, and **where to watch it** (streaming platforms: Jikan and AniList for anime, TMDB and JustWatch for the others, for the country each user picks in the account page).
- **Watchlist ("My List")**: add and remove titles, filter by genre and episode duration, sort by date, title or duration. The list only holds what you still want to watch: no status, rating or notes.
- **Random pick**: "Surprise Me" picks from the whole list, "Find me something like..." narrows it down by genre and duration first. It avoids repeating the previous pick.
- **Add an anime by hand** (`/anime/new`) when the providers do not have it. It is private to you, limited to 200 per user, and removing it from your list deletes it.
- **Languages**: English and French (footer switch, cookie, else the language of the browser); titles and synopses stay as the providers give them.
- **Accounts** (ASP.NET Core Identity, cookies): sign up, log in, log out, email confirmation, password reset by email, password change, account deletion, and a privacy page. Searching is open to everyone; the list, the picker and the account page need an account.
- **Adult content** is hidden by default. It can be turned on in **My account** (with a confirmation of being 18 or older); a switch next to the search and the random pick can then hide it again for the visit. Adult anime are flagged by the providers and carry an "18+" mark.
- A **footer** with the credits, the data sources and the version of the site with the commit it was built from (for example `v0.3.0 (d386362)`).

## Tech stack

- Blazor Web App (.NET 10, Interactive Server render mode; login, register and account pages are rendered by the server because they use the auth cookie)
- Entity Framework Core with MySQL (Pomelo provider)
- Jikan API and AniList API (GraphQL) for anime data, with retries, short timeouts and an automatic fallback; TMDB API for movies and TV series
- .NET Aspire for local orchestration
- xUnit for tests, run with a secret scan on every push and pull request by GitHub Actions
- Hosting and production database: AlwaysData

## Architecture

The solution (`WhatchaWatching.slnx`) follows a layered Clean Architecture:

| Project | Role |
| --- | --- |
| `WhatchaWatching` | User interface and Blazor components, accounts (Identity) |
| `WhatchaWatching.Application` | Services (anime, watchlist, random picker, anime added by hand, user settings) and mappers |
| `WhatchaWatching.Infrastructure` | Entities, EF Core configurations and migrations |
| `WhatchaWatching.Middleware` | Anime providers: Jikan and AniList clients, the fallback between them |
| `WhatchaWatching.Common` | DTOs, constants and enums |
| `WhatchaWatching.AppHost` | .NET Aspire orchestrator |
| `WhatchaWatching.Tests` | xUnit tests |

Dependencies: Web → Application → (Infrastructure, Middleware) → Common. The one exception is `UserEntity`, which the web project uses to set up Identity.

The data model is described in [docs/DATA_MODEL.md](docs/DATA_MODEL.md).

## Getting started

Prerequisites: the .NET 10 SDK, and either a MySQL (or MariaDB) database or a container runtime such as Docker Desktop (Aspire starts a MySQL container for you).

```bash
# Build
dotnet build

# Run the tests (they need no database and no network)
dotnet test

# Run everything via Aspire, with a MySQL container (dashboard at http://localhost:15209)
dotnet run --project WhatchaWatching.AppHost --launch-profile http

# Run the web app alone against a database you configured (http://localhost:5209)
dotnet run --project WhatchaWatching --launch-profile http

# Publish
dotnet publish WhatchaWatching -c Release
```

Aspire needs a container runtime. Without one, the `mysql` resource never starts and the site cannot reach its database: install Docker Desktop, or install MySQL yourself and run the web app alone.

### Database

The schema is managed with EF Core migrations, which are **not** applied automatically. A new local database is empty until you apply them:

```bash
dotnet tool restore
dotnet ef database update --project WhatchaWatching.Infrastructure
```

This applies them to the database named by the connection string in your user secrets: check that it is not the production one before running it.

## Configuration

Secrets must not be committed. Provide them through user secrets or environment variables.

| Key | Meaning |
| --- | --- |
| `ConnectionStrings:DefaultConnection` | The MySQL connection string (injected by Aspire when it runs the database). |
| `Database:ServerVersion` | Optional, for example `8.0.36-mysql`. Skips the version detection that connects to the database; without it a database outage breaks every page that uses a service. |
| `Tmdb:AccessToken` | The TMDB "API Read Access Token". Without it the site starts, but movies and TV series are reported as unavailable. |
| `Email:Host`, `Port`, `UserName`, `Password`, `From`, `FromName` | The mailbox that sends the confirmation and password reset links (STARTTLS, port 587). Without it nothing is sent. |

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "<value>" --project WhatchaWatching
```

In production, the same settings are environment variables of the site (`ConnectionStrings__DefaultConnection`, `Database__ServerVersion`, `Tmdb__AccessToken`, `Email__Host`...).

## Continuous integration and secret scan

`.github/workflows/ci.yml` runs on every push to `main` and every pull request: build and tests in Release, a check for vulnerable packages, and a [gitleaks](https://github.com/gitleaks/gitleaks) scan of the history. It does not deploy.

Locally, git hooks in `.githooks/` run the same secret scan before each commit and push. On a new clone, install gitleaks and run `git config core.hooksPath .githooks` (see [docs/OPERATIONS.md](docs/OPERATIONS.md)).

## Deployment

The site runs on AlwaysData. Two Claude Code skills in `.claude/skills` automate the steps; each one shows what it will do and asks for confirmation first:

- `/apply-migrations` previews the SQL of the pending migrations and applies them to the production database.
- `/deploy-alwaysdata` builds, tests, publishes, uploads over SFTP, restarts the site and checks it answers. It needs an SSH key registered at AlwaysData and the `ALWAYSDATA_API_TOKEN` environment variable; its non-secret settings are in `.claude/skills/deploy-alwaysdata/config.json` (not committed).
- `/commit-push` commits and pushes with a detailed message, and raises the version of the site (`<Version>` in `WhatchaWatching/WhatchaWatching.csproj`, shown in the footer) from what the commit changes: a new feature raises the minor number, a fix the patch number, docs and tests alone nothing.

When a release adds a migration, **apply the migration first, then deploy**: the previous code ignores a new column or table, but the new code needs it.

## Data sources

Anime data comes from [Jikan](https://jikan.moe/) (an unofficial MyAnimeList API) and [AniList](https://anilist.co/); movie and TV series data from [TMDB](https://www.themoviedb.org/), with streaming availability by JustWatch. Streaming links point to the platforms' own pages; availability depends on the country.
