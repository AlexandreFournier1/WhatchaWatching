namespace WhatchaWatching.Common.Dtos;

/// <param name="Results">One page of results.</param>
/// <param name="IsLocalOnly">
/// True when no anime provider answered and the results come from the anime already stored on this site:
/// the list may be incomplete.
/// </param>
public record AnimeSearchResultDto(PagedResultDto<AnimeSummaryDto> Results, bool IsLocalOnly);
