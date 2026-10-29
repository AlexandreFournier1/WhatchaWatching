# Application (`WhatchaWatching.Application`)

Services (with interfaces), mappers and queries. Repo-wide rules are in the root [CLAUDE.md](../CLAUDE.md).

- Every service has an interface (`IXxxService`); the UI depends on the interface only.
- Data access goes through `IDbContextFactory<ApplicationDbContext>`: one short-lived context per operation, `Select` projections and `AsNoTracking` for reads, EF Core async APIs.
- Services take a `MediaType`, because provider ids repeat from one type to another. A title is identified by its type and its provider id (the Jikan id for an anime). Anime come from `IAnimeProvider` (Jikan, then AniList), movies and TV series from `ITmdbClient`; `Manga` throws `NotSupportedException`.
- Anime are cached in the database for 7 days (`Media.LastSyncedAt`).
- Adult anime are hidden unless the user turned `ShowAdultContent` on; the session switch can hide them again.
- A custom anime (`ICustomAnimeService`) is private to its author: negative id, `Media.CreatedByUserId` set. Every query that can reach it must filter by the current user.
- The watchlist has no status, rating or notes: it only holds what the user still wants to watch.
- Register everything in this layer's `DependencyInjection.cs`. `AddApplication(IConfiguration)` calls `AddInfrastructure(...)` and `AddMiddleware()`.
- Use the `architecture-reviewer` agent after adding a service or changing layer references.
