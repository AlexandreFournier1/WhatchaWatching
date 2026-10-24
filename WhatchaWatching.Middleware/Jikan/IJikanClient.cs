using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Middleware.Jikan;

public interface IJikanClient : IAnimeProvider
{
    Task<IReadOnlyList<ExternalGenreDto>> GetGenresAsync(CancellationToken cancellationToken = default);
}
