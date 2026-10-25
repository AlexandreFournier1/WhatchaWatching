using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Middleware.AniList.Models;

namespace WhatchaWatching.Middleware.AniList;

public sealed class AniListClient(HttpClient httpClient) : IAniListClient
{
    private const string MediaFields =
        "idMal title { romaji english native } description coverImage { large } episodes duration status " +
        "averageScore seasonYear season startDate { year month day } genres isAdult";

    // Used by the details only: a search does not need the platforms of twelve anime.
    private const string LinkFields = " externalLinks { site url type isDisabled }";

    // AniList hides adult anime unless the argument is absent: "isAdult: null" still hides them, so it is left out of
    // the query to include them.
    private const string SearchQueryStart =
        "query ($search: String, $page: Int, $perPage: Int) { Page(page: $page, perPage: $perPage) { " +
        "pageInfo { total currentPage lastPage } media(search: $search, type: ANIME, ";

    private const string SearchQueryEnd = "sort: SEARCH_MATCH) { " + MediaFields + " } } }";

    private const string SafeSearchQuery = SearchQueryStart + "isAdult: false, " + SearchQueryEnd;
    private const string AdultSearchQuery = SearchQueryStart + SearchQueryEnd;

    // The details always include adult anime and say so (IsAdult): the service decides who may see them.
    private const string DetailsQuery =
        "query ($id: Int) { Media(idMal: $id, type: ANIME) { " + MediaFields + LinkFields + " } }";

    // The default web options read the camelCase names AniList uses.
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(
        string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var currentPage = Math.Max(page, 1);

        var variables = new { search = query.Trim(), page = currentPage, perPage = ExternalApiConstants.SEARCH_PAGE_SIZE };
        var response = await PostAsync<AniListPageData>(
            includeAdult ? AdultSearchQuery : SafeSearchQuery, variables, cancellationToken);

        var pageData = response?.Page;
        var items = (pageData?.Media ?? [])
            .Select(AniListMapper.ToDto)
            .OfType<ExternalAnimeDto>()
            .ToList();

        return new PagedResultDto<ExternalAnimeDto>(
            items,
            pageData?.PageInfo?.CurrentPage ?? currentPage,
            pageData?.PageInfo?.LastPage ?? 1,
            pageData?.PageInfo?.Total ?? items.Count);
    }

    public async Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var response = await PostAsync<AniListMediaData>(
            DetailsQuery, new { id = externalId }, cancellationToken, notFoundIsNull: true);

        return response?.Media is { } media ? AniListMapper.ToDto(media) : null;
    }

    private async Task<T?> PostAsync<T>(
        string query, object variables, CancellationToken cancellationToken, bool notFoundIsNull = false)
        where T : class
    {
        try
        {
            using var response = await httpClient.PostAsJsonAsync(string.Empty, new { query, variables }, JsonOptions, cancellationToken);

            // AniList answers 404 for an id it does not know.
            if (notFoundIsNull && response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new ExternalApiException($"AniList answered {(int)response.StatusCode}.", response.StatusCode);
            }

            var body = await response.Content.ReadFromJsonAsync<AniListResponse<T>>(JsonOptions, cancellationToken);

            if (body?.Data is null && body?.Errors is { Count: > 0 } errors)
            {
                throw new ExternalApiException($"AniList returned an error: {errors[0].Message}");
            }

            return body?.Data;
        }
        catch (HttpRequestException ex)
        {
            throw new ExternalApiException("AniList could not be reached.", ex.StatusCode, ex);
        }
        catch (JsonException ex)
        {
            throw new ExternalApiException("AniList returned an unreadable answer.", innerException: ex);
        }
        catch (ExecutionRejectedException ex)
        {
            // The resilience pipeline gave up: timeout or open circuit breaker.
            throw new ExternalApiException("AniList is not available.", innerException: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new ExternalApiException("AniList timed out.", innerException: ex);
        }
    }
}
