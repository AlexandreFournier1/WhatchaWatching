using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb;

namespace WhatchaWatching.Tests.TestSupport;

public sealed class FakeTmdbClient : ITmdbClient
{
    private readonly Dictionary<(MediaType, int), ExternalMediaDto> _titles = [];

    public int GetCalls { get; private set; }

    public int SearchCalls { get; private set; }

    /// <summary>What the last search asked for, like a client that honors it.</summary>
    public bool? LastIncludeAdult { get; private set; }

    /// <summary>When set, every call throws it (a TMDB outage).</summary>
    public TmdbApiException? Failure { get; set; }

    public static ExternalMediaDto Movie(int id, string? title = null, int? minutes = 120, params (int Id, string Name)[] genres) => new(
        MediaType.Movie,
        id,
        title ?? $"Movie {id}",
        null,
        "A synopsis.",
        $"https://img.test/m{id}.jpg",
        null,
        minutes,
        MediaStatus.Finished,
        7.5m,
        2020,
        null,
        null,
        new DateTime(2020, 4, 1, 0, 0, 0, DateTimeKind.Utc),
        genres.Select(g => new ExternalGenreDto(g.Id, g.Name)).ToList());

    public static ExternalMediaDto Series(int id, string? title = null, int? minutes = 45, params (int Id, string Name)[] genres) => new(
        MediaType.TVShow,
        id,
        title ?? $"Series {id}",
        null,
        "A synopsis.",
        $"https://img.test/s{id}.jpg",
        24,
        minutes,
        MediaStatus.Airing,
        8m,
        2021,
        null,
        null,
        new DateTime(2021, 1, 1, 0, 0, 0, DateTimeKind.Utc),
        genres.Select(g => new ExternalGenreDto(g.Id, g.Name)).ToList())
    {
        SeasonCount = 2
    };

    public FakeTmdbClient With(params ExternalMediaDto[] titles)
    {
        foreach (var title in titles)
        {
            _titles[(title.Type, title.ExternalId)] = title;
        }

        return this;
    }

    public Task<PagedResultDto<ExternalMediaDto>> SearchAsync(
        MediaType type, string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        SearchCalls++;
        LastIncludeAdult = includeAdult;
        if (Failure is not null)
        {
            throw Failure;
        }

        var items = _titles.Values
            .Where(t => t.Type == type && t.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .Where(t => includeAdult || !t.IsAdult)
            .ToList();

        return Task.FromResult(new PagedResultDto<ExternalMediaDto>(items, page, 1, items.Count));
    }

    /// <summary>What the extras of any title are (empty by default); counts the calls.</summary>
    public MediaExtrasDto Extras { get; set; } = MediaExtrasDto.Empty;

    public int ExtrasCalls { get; private set; }

    public Task<MediaExtrasDto?> GetExtrasAsync(MediaType type, int externalId, CancellationToken cancellationToken = default)
    {
        ExtrasCalls++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult<MediaExtrasDto?>(_titles.ContainsKey((type, externalId)) ? Extras : null);
    }

    public Task<ExternalMediaDto?> GetAsync(MediaType type, int externalId, CancellationToken cancellationToken = default)
    {
        GetCalls++;
        if (Failure is not null)
        {
            throw Failure;
        }

        return Task.FromResult(_titles.GetValueOrDefault((type, externalId)));
    }
}
