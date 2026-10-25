using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Middleware.Jikan.Models;

namespace WhatchaWatching.Middleware.Jikan;

public sealed class JikanClient(HttpClient httpClient) : IJikanClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<PagedResultDto<ExternalAnimeDto>> SearchAnimeAsync(
        string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var currentPage = Math.Max(page, 1);

        var url = $"anime?q={Uri.EscapeDataString(query.Trim())}&page={currentPage}" +
                  $"&limit={ExternalApiConstants.SEARCH_PAGE_SIZE}" + (includeAdult ? string.Empty : "&sfw=true");
        var response = await GetAsync<JikanListResponse<JikanAnime>>(url, cancellationToken);

        var items = (response?.Data ?? []).Select(JikanMapper.ToDto).ToList();
        var pagination = response?.Pagination;

        return new PagedResultDto<ExternalAnimeDto>(
            items,
            pagination?.CurrentPage ?? currentPage,
            pagination?.LastVisiblePage ?? 1,
            pagination?.Items?.Total ?? items.Count);
    }

    public async Task<ExternalAnimeDto?> GetAnimeAsync(int externalId, CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<JikanResponse<JikanAnime>>($"anime/{externalId}", cancellationToken, notFoundIsNull: true);
        if (response?.Data is not { } anime)
        {
            return null;
        }

        return JikanMapper.ToDto(anime) with { StreamingLinks = await GetStreamingLinksAsync(externalId, cancellationToken) };
    }

    // The links are a bonus: when Jikan cannot give them, the anime is still returned without.
    private async Task<IReadOnlyList<StreamingLinkDto>> GetStreamingLinksAsync(int externalId, CancellationToken cancellationToken)
    {
        try
        {
            var response = await GetAsync<JikanListResponse<JikanStreamingLink>>(
                $"anime/{externalId}/streaming", cancellationToken, notFoundIsNull: true);

            return StreamingLinkFilter.Clean((response?.Data ?? []).Select(l => (l.Name, l.Url)));
        }
        catch (ExternalApiException)
        {
            return [];
        }
    }

    public async Task<IReadOnlyList<ExternalGenreDto>> GetGenresAsync(CancellationToken cancellationToken = default)
    {
        var response = await GetAsync<JikanListResponse<JikanGenre>>("genres/anime", cancellationToken);

        return JikanMapper.ToGenres(response?.Data);
    }

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken, bool notFoundIsNull = false)
        where T : class
    {
        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (notFoundIsNull && response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new JikanApiException(
                    $"Jikan answered {(int)response.StatusCode} for '{url}'.", response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new JikanApiException($"Jikan could not be reached for '{url}'.", ex.StatusCode, ex);
        }
        catch (JsonException ex)
        {
            throw new JikanApiException($"Jikan returned an unreadable answer for '{url}'.", innerException: ex);
        }
        catch (ExecutionRejectedException ex)
        {
            // The resilience pipeline gave up: timeout or open circuit breaker.
            throw new JikanApiException($"Jikan is not available for '{url}'.", innerException: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new JikanApiException($"Jikan timed out for '{url}'.", innerException: ex);
        }
    }
}
