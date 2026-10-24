# WhatchaWatching

WhatchaWatching is a Blazor Web App (.NET 10) that retrieves anime from the [Jikan API](https://jikan.moe/) and lets users build and manage their own watchlist, stored in a database. A random selection tool picks the next anime to watch from the list, with filtering options to narrow down the results.

**Live site:** [https://whatcha-watching.alwaysdata.net](https://whatcha-watching.alwaysdata.net)

> The project is under construction: the UI is built (Home, My List, Search, Anime detail, account pages) but runs on placeholder data (`Components/Preview/PreviewWatchList.cs`). Jikan, the database and authentication are not connected yet.

## Tech stack

- Blazor Web App (.NET 10, Interactive Server render mode)
- Entity Framework Core with MySQL (Pomelo provider)
- Jikan API for anime data, TMDB API for movies and TV series
- .NET Aspire for local orchestration
- xUnit for tests
- Hosting and production database: AlwaysData

## Architecture

The solution (`WhatchaWatching.slnx`) follows a layered Clean Architecture:

| Project | Role |
| --- | --- |
| `WhatchaWatching` | User interface and Blazor components |
| `WhatchaWatching.Application` | Services and mappers |
| `WhatchaWatching.Infrastructure` | Entities and EF Core configurations |
| `WhatchaWatching.Middleware` | External API calls (Jikan, TMDB) |
| `WhatchaWatching.Common` | DTOs, constants and enums |
| `WhatchaWatching.AppHost` | .NET Aspire orchestrator |
| `WhatchaWatching.Tests` | xUnit tests |

Dependencies: Web → Application → (Infrastructure, Middleware) → Common.

## Getting started

Prerequisites: .NET 10 SDK and a MySQL database.

```bash
# Build
dotnet build

# Run tests
dotnet test

# Run everything via Aspire (dashboard at http://localhost:17069)
dotnet run --project WhatchaWatching.AppHost --launch-profile http

# Run the web app alone (http://localhost:7069, http://localhost:5209)
dotnet run --project WhatchaWatching --launch-profile http

# Publish
dotnet publish WhatchaWatching -c Release
```

## Configuration

Secrets such as the database connection string and the TMDB API key must not be committed. Provide them through configuration files excluded from source control, user secrets or environment variables.
