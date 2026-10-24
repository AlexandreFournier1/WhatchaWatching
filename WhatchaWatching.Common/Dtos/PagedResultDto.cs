namespace WhatchaWatching.Common.Dtos;

public record PagedResultDto<T>(IReadOnlyList<T> Items, int Page, int TotalPages, int TotalItems)
{
    public bool HasNextPage => Page < TotalPages;
}
