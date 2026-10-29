using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Polly;
using WhatchaWatching.Common.Constants;
using WhatchaWatching.Common.Dtos;
using WhatchaWatching.Common.Enum;
using WhatchaWatching.Middleware.Tmdb.Models;

namespace WhatchaWatching.Middleware.Tmdb;

public sealed class TmdbClient(HttpClient httpClient, TmdbOptions options) : ITmdbClient
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower
    };

    public async Task<PagedResultDto<ExternalMediaDto>> SearchAsync(
        MediaType type, string query, int page, bool includeAdult = false, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        var currentPage = Math.Max(page, 1);

        var url = $"search/{PathFor(type)}?query={Uri.EscapeDataString(query.Trim())}&page={currentPage}" +
                  $"&language={ExternalApiConstants.TMDB_LANGUAGE}&include_adult={(includeAdult ? "true" : "false")}";
        var response = await GetAsync<TmdbSearchResponse>(url, cancellationToken);

        var items = (response?.Results ?? []).Select(t => TmdbMapper.ToDto(t, type)).ToList();

        return new PagedResultDto<ExternalMediaDto>(
            items,
            response?.Page > 0 ? response.Page : currentPage,
            Math.Max(response?.TotalPages ?? 1, 1),
            response?.TotalResults ?? items.Count);
    }

    public async Task<ExternalMediaDto?> GetAsync(MediaType type, int externalId, CancellationToken cancellationToken = default)
    {
        // The streaming platforms come in the same answer: no second request that could fail on its own.
        var url = $"{PathFor(type)}/{externalId}?language={ExternalApiConstants.TMDB_LANGUAGE}&append_to_response=watch/providers";
        var title = await GetAsync<TmdbTitle>(url, cancellationToken, notFoundIsNull: true);

        return title is null ? null : TmdbMapper.ToDto(title, type);
    }

    public async Task<MediaExtrasDto?> GetExtrasAsync(MediaType type, int externalId, CancellationToken cancellationToken = default)
    {
        var url = $"{PathFor(type)}/{externalId}?language={ExternalApiConstants.TMDB_LANGUAGE}" +
                  "&append_to_response=videos,credits,recommendations";
        var title = await GetAsync<TmdbTitle>(url, cancellationToken, notFoundIsNull: true);

        return title is null ? null : TmdbMapper.ToExtras(title, type);
    }

    private static string PathFor(MediaType type) => type switch
    {
        MediaType.Movie => "movie",
        MediaType.TVShow => "tv",
        _ => throw new ArgumentOutOfRangeException(nameof(type), type, "TMDB only provides movies and TV series.")
    };

    private async Task<T?> GetAsync<T>(string url, CancellationToken cancellationToken, bool notFoundIsNull = false)
        where T : class
    {
        if (!options.IsConfigured)
        {
            throw new TmdbNotConfiguredException();
        }

        try
        {
            using var response = await httpClient.GetAsync(url, cancellationToken);

            if (notFoundIsNull && response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new TmdbApiException(
                    $"TMDB answered {(int)response.StatusCode} for '{url}'.", response.StatusCode);
            }

            return await response.Content.ReadFromJsonAsync<T>(JsonOptions, cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            throw new TmdbApiException($"TMDB could not be reached for '{url}'.", ex.StatusCode, ex);
        }
        catch (JsonException ex)
        {
            throw new TmdbApiException($"TMDB returned an unreadable answer for '{url}'.", innerException: ex);
        }
        catch (ExecutionRejectedException ex)
        {
            // The resilience pipeline gave up: timeout or open circuit breaker.
            throw new TmdbApiException($"TMDB is not available for '{url}'.", innerException: ex);
        }
        catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new TmdbApiException($"TMDB timed out for '{url}'.", innerException: ex);
        }
    }
}
