using WhatchaWatching.Common.Dtos;

namespace WhatchaWatching.Components.Preview;

/// <summary>
/// Temporary in-memory stand-in used while the UI is built without a backend.
/// Replace it with the real watchlist and Jikan services once they exist.
/// </summary>
public class PreviewWatchList
{
    public static readonly IReadOnlyList<string> DurationOptions = ["Under 15 min", "15 - 25 min", "Over 25 min"];

    public static readonly IReadOnlyList<string> SortOptions = ["Recently added", "Title (A-Z)", "Shortest first"];

    private static readonly List<AnimeSummaryDto> Catalog =
    [
        Sample(1, "Sample Anime One", "Fantasy,Romance", 24, 12, 2021, 8.1m, "Finished"),
        Sample(2, "Sample Anime Two", "Action,Sci-Fi", 23, 24, 2019, 7.9m, "Finished"),
        Sample(3, "Sample Anime Three", "Comedy,Slice of Life", 12, 13, 2022, 7.4m, "Finished"),
        Sample(4, "Sample Anime Four", "Drama,Romance", 25, 26, 2018, 8.6m, "Finished"),
        Sample(5, "Sample Anime Five", "Fantasy,Action", 24, 50, 2023, 8.3m, "Airing"),
        Sample(6, "Sample Anime Six", "Comedy", 5, 24, 2020, 7.0m, "Finished"),
        Sample(7, "Sample Anime Seven", "Sci-Fi,Drama", 28, 12, 2024, null, "Airing"),
        Sample(8, "Sample Anime Eight", "Slice of Life,Romance", 24, 12, 2017, 7.7m, "Finished"),
        Sample(9, "Sample Anime Nine", "Action,Comedy", 22, 39, 2016, 8.0m, "Finished"),
        Sample(10, "Sample Anime Ten", "Fantasy,Drama", 45, 8, 2025, null, "Upcoming")
    ];

    private readonly List<int> _listedIds = [1, 4, 5, 8];

    /// <summary>Fake network delay so the loading states can be seen and designed.</summary>
    public static Task SimulateLatency() => Task.Delay(Random.Shared.Next(400, 800));

    public IReadOnlyList<string> Genres { get; } = Catalog
        .SelectMany(a => a.Genres)
        .Distinct()
        .Order()
        .ToList();

    /// <summary>Items of the list, most recently added first.</summary>
    public IReadOnlyList<AnimeSummaryDto> Items =>
        Enumerable.Reverse(_listedIds).Select(id => Catalog.First(a => a.Id == id)).ToList();

    public AnimeSummaryDto? Find(int id) => Catalog.FirstOrDefault(a => a.Id == id);

    public IReadOnlyList<AnimeSummaryDto> Search(string query) =>
        Catalog.Where(a => a.Title.Contains(query, StringComparison.OrdinalIgnoreCase)).ToList();

    public bool Contains(int id) => _listedIds.Contains(id);

    public void Add(int id)
    {
        if (!Contains(id))
        {
            _listedIds.Add(id);
        }
    }

    public void Remove(int id) => _listedIds.Remove(id);

    public static bool MatchesDuration(AnimeSummaryDto anime, string? duration) =>
        duration switch
        {
            "Under 15 min" => anime.EpisodeDurationMinutes < 15,
            "15 - 25 min" => anime.EpisodeDurationMinutes is >= 15 and <= 25,
            "Over 25 min" => anime.EpisodeDurationMinutes > 25,
            _ => true
        };

    public static bool MatchesGenres(AnimeSummaryDto anime, IReadOnlyCollection<string> genres) =>
        genres.Count == 0 || anime.Genres.Any(genres.Contains);

    private static AnimeSummaryDto Sample(
        int id, string title, string genres, int minutes, int episodes, int year, decimal? score, string status) =>
        new(id, title, null,
            "This is a placeholder synopsis. Real descriptions will come from Jikan once the data layer is connected.",
            genres.Split(','), minutes, episodes, year, score, status);
}
