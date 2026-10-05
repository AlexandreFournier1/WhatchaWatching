# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project: WhatchaWatching

### Purpose

WhatchaWatching is a Blazor Web App (.NET 10, `net10.0`) that will retrieves anime from *Jikan* (external API) and allows users to build and manage their own watchlist, with their selections stored in a database. It will also provides a random selection tool that picks the next anime to watch from their list, with filtering options to help narrow down the results.

For now, a Blazor template is used before implementing the real project.

The solution file is `WhatchaWatching.slnx` (new XML solution format). It holds two projects: the web app in `WhatchaWatching/` and the .NET Aspire orchestrator in `WhatchaWatching.AppHost/`.

## Commands

Run from the repo root (or `WhatchaWatching/`):

- Build: `dotnet build`
- Tests: `dotnet test` (run before every commit)
- Run everything via Aspire: `dotnet run --project WhatchaWatching.AppHost --launch-profile http` (dashboard at http://localhost:17069)
- Run the web app alone: `dotnet run --project WhatchaWatching --launch-profile http` (http://localhost:7069, http://localhost:5209)
- Hot reload: `dotnet watch --project WhatchaWatching`
- Publish: `dotnet publish WhatchaWatching -c Release`

`bin/` and `obj/` contain Release build output. They are build artifacts, so ignore them.

## Architecture

The app follow a Clean Architecture with multiple layers : 
- **WhatchaWatching** : user interface and UI components
- **WhatchaWatching.Infrastructure** : entities and configurations
- **WhatchaWatching.Common** : dtos, constants and enums
- **WhatchaWatching.Application** : services and mappers
- **WhatchaWatching.Middleware** : call API and retreive datas

- **WhatchaWatching.Tests** : xunit tests
- **Dependencies** : Web → Application → (Infrastructure, Middleware) → Common. Each layer exposes an `Add<Layer>()` extension in its `DependencyInjection.cs`. `AddApplication()` calls the other two, and the web app's `Program.cs` calls only `AddApplication()`.

- **Abstraction** : each services has an interface that they implement

- **Aspire AppHost**: [Program.cs](WhatchaWatching.AppHost/Program.cs) is the single place that declares the resources to launch. Add new services, databases or containers there with `builder.Add...`, and chain `.WithReference(...)` to wire them to the web app. The project uses the `Aspire.AppHost.Sdk/13.6.0` SDK, so it needs no separate Aspire package reference. `Projects.WhatchaWatching` is source-generated from the project reference.

- **Render mode**: [Program.cs](WhatchaWatching/Program.cs) registers only `AddInteractiveServerComponents()` / `AddInteractiveServerRenderMode()`. This is Blazor Server (SignalR circuit) only, with no WebAssembly client project. Pages are interactive only if they opt in with `@rendermode InteractiveServer` (or it is set globally in `App.razor`). Check there before assuming a component is interactive.
- **Component tree**: `Components/App.razor` (HTML shell) → `Routes.razor` (Router, `NotFoundPage` = `Pages.NotFound`) → `Layout/MainLayout.razor` → pages in `Components/Pages/`. Shared `@using`s go in `Components/_Imports.razor`.
- **Error and 404 handling**: non-Development uses `/Error` with HSTS. `UseStatusCodePagesWithReExecute("/not-found")` backs the `NotFound` page.
- **Styling**: Bootstrap is vendored in `wwwroot/lib/bootstrap`. Global CSS is `wwwroot/app.css`. Components use CSS isolation (`*.razor.css`). `ReconnectModal` has a collocated `.razor.js`.
- **Static assets**: served with `app.MapStaticAssets()` (not `UseStaticFiles`).
- Nullable reference types and implicit usings are enabled. `BlazorDisableThrowNavigationException` is set in the csproj.

## Database

The application uses **MySQL** as its database.

* Use **Entity Framework Core** for database access.
* Use **Pomelo.EntityFrameworkCore.MySql** as the EF Core MySQL provider.
* Do not use SQL Server, PostgreSQL or SQLite-specific features unless explicitly requested.
* Use EF Core migrations for database schema changes.
* Do not manually modify the database schema when an EF Core migration can handle the change.
* Use asynchronous database operations whenever possible.
* Avoid unnecessary database queries and only retrieve the data that is required.
* Use appropriate indexes, foreign keys and constraints.
* Keep database access logic inside dedicated services or the appropriate data-access layer rather than directly inside Blazor components.
* Never hard-code database credentials or connection strings containing secrets in source code.
* Store sensitive database configuration in configuration files or environment variables.
* The production database is hosted on **AlwaysData** and uses MySQL.

## External APIs

### TMDB

The application uses the **TMDB API (The Movie Database)** to retrieve information about movies and TV series.

* Use TMDB for **movies and TV series**.
* Use **Jikan API** for anime-related data.
* Keep TMDB API calls inside dedicated services. Do not call TMDB directly from Blazor components.
* Use the TMDB API to retrieve movie and TV series information such as:
  * Titles
  * Original titles
  * Overview / synopsis
  * Release dates
  * Genres
  * Ratings
  * Posters
  * Backdrops
  * Cast and crew
  * Videos / trailers
  * Watch providers
* Search for movies and TV series by title using TMDB's search endpoints.
* Use the TMDB ID as the external identifier when storing TMDB content in the database.
* Do not assume that every field is present in a TMDB response. Handle missing or null values appropriately.
* Handle API errors, timeouts and rate limits gracefully.
* Avoid unnecessary API calls. Reuse data already stored in the local database when appropriate.
* Do not hard-code the TMDB API key in source code.
* Store the TMDB API key in configuration or environment variables.
* Do not use TMDB for anime when Jikan provides the required anime data.

### Jikan

The application uses the **Jikan API** to retrieve anime-related information.

* Use Jikan for **anime**.
* Keep Jikan API calls inside dedicated services.
* Use the MyAnimeList/Jikan ID as the external identifier for anime.
* Handle missing data, API errors, timeouts and rate limits.
* Avoid unnecessary API calls and reuse locally stored data when appropriate.