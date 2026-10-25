using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware;

namespace WhatchaWatching.Tests.TestSupport;

public sealed class FakeAnimeProvider : IAnimeProvider
{
    private readonly Dictionary<int, ExternalAnimeDto> _animes = [];

    public int GetAnimeCalls { get; private set; }

    /// <summary>What the last search asked for, like a provider that honors it.</summary>
    public bool? LastIncludeAdult { get; private set; }

    /// <summary>When set, every call throws it (a Jikan outage).</summary>
    public ExternalApiException? Failure { get; set; }

    public static ExternalAnimeDto Anime(
        int id,
        string? title = null,
        int? minutes = 24,
        params string[] genres) => new(
            id,
            title ?? $"Anime {id}",
            null,
            "A synopsis.",
            $"https://img.test/{id}.jpg",
            12,
            minutes,
            MediaStatus.Finished,
            7.5m,
            2020,
            "spring",
            "PG-13",
            new DateTime(2020, 4, 1, 0, 0, 0, DateTimeKind.Utc),
            genres.Select(g => new ExternalGenreDto(GenreIds[g], g)).ToList());

    private static readonly Dictionary<string, int> GenreIds = new()
    {
        ["Action"] = 1,
        ["Adventure"] = 2,
        ["Comedy"] = 4,
        ["Drama"] = 8,
        ["Fantasy"] = 10,
        ["Romance"] = 22
    };

    public FakeAnimeProvider With(params ExternalAnimeDto[] animes)
    {
        foreach (var anime in animes)
        {
            _animes[anime.ExternalId] = anime;
        }

        return this;
    }

    public Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        LastIncludeAdult = includeAdult;
        ThrowIfDown();

        var matches = _animes.Values
            .Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase) && (includeAdult || !a.IsAdult))
            .ToList();

        return Task.FromResult(new PagedResultDto<ExternalAnimeDto>(matches, page, 2, matches.Count));
    }

    public Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default)
    {
        GetAnimeCalls++;
        ThrowIfDown();

        return Task.FromResult(_animes.GetValueOrDefault(externalId));
    }


    private void ThrowIfDown()
    {
        if (Failure is not null)
        {
            throw Failure;
        }
    }
}
