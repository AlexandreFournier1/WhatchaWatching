# Middleware (`WhatchaWatching.Middleware`)

HTTP clients for Jikan, AniList and TMDB. Repo-wide rules are in the root [CLAUDE.md](../CLAUDE.md).

## Jikan and AniList (anime)

- Use Jikan for **all anime data**. Use the MyAnimeList id as the external identifier everywhere. AniList (GraphQL, no key) is the fallback: `FallbackAnimeProvider` asks Jikan first, then AniList when Jikan fails (not when it says "not found"), and skips Jikan for 60 seconds after a failure (`ProviderCooldown`, a singleton). AniList anime are matched through their `idMal`; AniList genres have no id and are translated by name to the MyAnimeList ids (`AniListMapper`). Services depend on `IAnimeProvider`, never on a single client.
- When no provider answers, `IMediaService.SearchAsync` searches the anime already stored in the database (`IsLocalOnly`) and the Search page says so; it throws `MediaProviderUnavailableException` only if nothing stored matches.
- Timeouts are short on purpose (5 s per attempt, 12 s total, one retry): a person is waiting.
- Keep calls in dedicated clients here, behind an interface.

## TMDB (movies and TV series)

- Use TMDB for movies and TV series only, never for anime that Jikan covers. Use the TMDB ID as the external identifier.
- Keep calls in dedicated clients here. Store the API key in configuration (user-secrets / environment variable), never in source.
- Data available: titles, original titles, overview, release dates, genres, ratings, posters, backdrops, cast and crew, videos, watch providers; search by title through the search endpoints.
- `Tmdb:AccessToken` is the "API Read Access Token" (sent as a Bearer token). Without it the site still starts and the movie/TV calls report TMDB as unavailable. The country whose streaming platforms are listed is a setting of each user (`Users.Country`, default `FR`, from `CountryCatalog`), not of the site.

## Rules for all providers

- Register clients with `IHttpClientFactory` (typed clients) and configure resilience (timeouts, retry, rate-limit handling, e.g. `Microsoft.Extensions.Http.Resilience`).
- Any field can be missing or `null` in a response: model it as nullable and handle it.
- Handle API errors, timeouts and rate limits gracefully; never let an external failure crash a page.
- Avoid redundant calls: reuse data already stored in the local database (`Media.LastSyncedAt`) when appropriate.
- Never call an external API from a Blazor component.
- Tests fake the HTTP handler; never call real endpoints.
