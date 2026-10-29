using Microsoft.Extensions.Logging.Abstractions;
using WhatchaWatching.Application.Services;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Components.Shared;
using WhatchaWatching.Infrastructure.Data.Entities;
using WhatchaWatching.Tests.TestSupport;

namespace WhatchaWatching.Tests;

/// <summary>The conditions of the list: titles are stored by hand so that each one has the values a test needs.</summary>
public class WatchListFilterTests
{
    private const int User = 1;

    private readonly TestDbContextFactory _db = new();
    private readonly WatchListService _watchList;
    private int _nextId = 1;

    public WatchListFilterTests()
    {
        var media = new MediaService(new FakeAnimeProvider(), new FakeTmdbClient(), _db, TimeProvider.System, NullLogger<MediaService>.Instance);
        _watchList = new WatchListService(_db, media);
    }

    private async Task ListAsync(
        string title, MediaType type, int? minutes = null, decimal? score = null, int? year = null, DateTime? addedAt = null)
    {
        await using var context = _db.CreateDbContext();
        var entity = new MediaEntity
        {
            Type = type,
            ExternalId = _nextId++,
            Title = title,
            Status = MediaStatus.Finished,
            EpisodeDurationMinutes = minutes,
            Score = score,
            Year = year
        };
        entity.WatchListItems.Add(new WatchListItemEntity { UserId = User });
        context.Media.Add(entity);
        await context.SaveChangesAsync();
    }

    private async Task<string[]> TitlesAsync(WatchListFilterDto filter, WatchListSort sort = WatchListSort.Title) =>
        (await _watchList.GetAsync(User, filter, sort)).Select(m => m.Title).ToArray();

    private static WatchListFilterDto Of(DurationRange range, MediaType? type = null) =>
        new WatchListFilterDto([], range) { Type = type };

    [Theory]
    [InlineData(MediaType.Anime, 14, DurationRange.Short)]
    [InlineData(MediaType.Anime, 24, DurationRange.Medium)]
    [InlineData(MediaType.Anime, 26, DurationRange.Long)]
    [InlineData(MediaType.TVShow, 29, DurationRange.Short)]
    [InlineData(MediaType.TVShow, 45, DurationRange.Medium)]
    [InlineData(MediaType.TVShow, 51, DurationRange.Long)]
    [InlineData(MediaType.Movie, 89, DurationRange.Short)]
    [InlineData(MediaType.Movie, 100, DurationRange.Medium)]
    [InlineData(MediaType.Movie, 121, DurationRange.Long)]
    public async Task Duration_IsJudgedAgainstTheLimitsOfTheTypeOfTheTitle(MediaType type, int minutes, DurationRange expected)
    {
        await ListAsync("Title", type, minutes);

        foreach (var range in Enum.GetValues<DurationRange>())
        {
            var found = await TitlesAsync(Of(range));

            Assert.Equal(range == expected ? 1 : 0, found.Length);
        }
    }

    [Fact]
    public async Task AMovieOf120Minutes_IsNoLongerLong_ButAnAnimeEpisodeOf120IsStill()
    {
        await ListAsync("Film", MediaType.Movie, 120);
        await ListAsync("Special", MediaType.Anime, 120);

        Assert.Equal(["Special"], await TitlesAsync(Of(DurationRange.Long)));
        Assert.Equal(["Film"], await TitlesAsync(Of(DurationRange.Medium)));
    }

    [Fact]
    public async Task ATitleWithoutADuration_NeverMatchesALength()
    {
        await ListAsync("Unknown", MediaType.Movie);

        foreach (var range in Enum.GetValues<DurationRange>())
        {
            Assert.Empty(await TitlesAsync(Of(range)));
        }
    }

    [Fact]
    public async Task Score_KeepsTheTitlesScoredAtLeastThat()
    {
        await ListAsync("Low", MediaType.Movie, score: 6.9m);
        await ListAsync("Seven", MediaType.Movie, score: 7m);
        await ListAsync("High", MediaType.Movie, score: 9.1m);
        await ListAsync("Unscored", MediaType.Movie);

        Assert.Equal(["High", "Seven"], await TitlesAsync(WatchListFilterDto.None with { MinScore = 7m }));
        Assert.Equal(["High"], await TitlesAsync(WatchListFilterDto.None with { MinScore = 9m }));
    }

    [Theory]
    [InlineData(ReleasePeriod.Since2020, new[] { "B2021", "A2020" })]
    [InlineData(ReleasePeriod.From2010To2019, new[] { "D2019", "C2010" })]
    [InlineData(ReleasePeriod.From2000To2009, new[] { "F2009", "E2000" })]
    [InlineData(ReleasePeriod.Before2000, new[] { "G1999" })]
    public async Task Period_KeepsTheTitlesOfThoseYears(ReleasePeriod period, string[] expected)
    {
        foreach (var (title, year) in new[] { ("A2020", 2020), ("B2021", 2021), ("C2010", 2010), ("D2019", 2019), ("E2000", 2000), ("F2009", 2009), ("G1999", 1999) })
        {
            await ListAsync(title, MediaType.Movie, year: year);
        }
        await ListAsync("NoYear", MediaType.Movie);

        var found = await TitlesAsync(WatchListFilterDto.None with { Period = period });

        Assert.Equal(expected.Order(), found.Order());
    }

    [Fact]
    public async Task TheConditions_AddUp()
    {
        await ListAsync("Match", MediaType.Movie, 130, 8.2m, 2022);
        await ListAsync("TooShort", MediaType.Movie, 80, 8.2m, 2022);
        await ListAsync("TooLow", MediaType.Movie, 130, 6m, 2022);
        await ListAsync("TooOld", MediaType.Movie, 130, 8.2m, 1995);

        var filter = new WatchListFilterDto([], DurationRange.Long) { Type = MediaType.Movie, MinScore = 8m, Period = ReleasePeriod.Since2020 };

        Assert.Equal(["Match"], await TitlesAsync(filter));
    }

    [Fact]
    public async Task Sort_ByScore_PutsTheBestFirstAndTheUnscoredLast()
    {
        await ListAsync("Middle", MediaType.Movie, score: 7m);
        await ListAsync("Best", MediaType.Movie, score: 9m);
        await ListAsync("Unscored", MediaType.Movie);
        await ListAsync("Worst", MediaType.Movie, score: 5m);

        Assert.Equal(["Best", "Middle", "Worst", "Unscored"], await TitlesAsync(WatchListFilterDto.None, WatchListSort.BestRated));
    }

    [Fact]
    public async Task Sort_ByYear_PutsTheNewestFirstAndTheUndatedLast()
    {
        await ListAsync("Old", MediaType.Movie, year: 1999);
        await ListAsync("Undated", MediaType.Movie);
        await ListAsync("New", MediaType.Movie, year: 2024);
        await ListAsync("Recent", MediaType.Movie, year: 2021);

        Assert.Equal(["New", "Recent", "Old", "Undated"], await TitlesAsync(WatchListFilterDto.None, WatchListSort.Newest));
    }

    [Fact]
    public void TheDropdowns_UseTheLimitsOfTheType()
    {
        Assert.Equal(["Under 15 min", "15 - 25 min", "Over 25 min"], FilterOptions.DurationValues(MediaType.Anime));
        Assert.Equal(["Under 30 min", "30 - 50 min", "Over 50 min"], FilterOptions.DurationValues(MediaType.TVShow));
        Assert.Equal(["Under 90 min", "90 - 120 min", "Over 120 min"], FilterOptions.DurationValues(MediaType.Movie));
    }

    [Fact]
    public void ToFilter_ReadsTheChoicesOfTheDropdowns()
    {
        var filter = FilterOptions.ToFilter(
            MediaType.Movie, ["Action"], ["Over 120 min"], ["8 and above"], ["2010 - 2019"]);

        Assert.Equal(MediaType.Movie, filter.Type);
        Assert.Equal(DurationRange.Long, filter.Duration);
        Assert.Equal(8m, filter.MinScore);
        Assert.Equal(ReleasePeriod.From2010To2019, filter.Period);
        Assert.Equal(["Action"], filter.Genres);
    }

    [Fact]
    public void ToFilter_IgnoresALengthOfAnotherType()
    {
        // The labels follow the type: "Over 25 min" is nothing for a movie.
        var filter = FilterOptions.ToFilter(MediaType.Movie, [], ["Over 25 min"]);

        Assert.Null(filter.Duration);
    }

    [Fact]
    public void ToSort_ReadsTheNewSorts()
    {
        Assert.Equal(WatchListSort.BestRated, FilterOptions.ToSort(["Best rated"]));
        Assert.Equal(WatchListSort.Newest, FilterOptions.ToSort(["Newest first"]));
        Assert.Equal(WatchListSort.RecentlyAdded, FilterOptions.ToSort([]));
    }
}
