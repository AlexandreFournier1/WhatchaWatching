namespace WhatchaWatching.Common.Dtos;

/// <param name="Results">One page of results.</param>
/// <param name="IsLocalOnly">
/// True when no provider answered and the results come from the titles already stored on this site:
/// the list may be incomplete.
/// </param>
public record MediaSearchResultDto(PagedResultDto<MediaSummaryDto> Results, bool IsLocalOnly);
