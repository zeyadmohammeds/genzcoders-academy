namespace GenZCoders.DTOs;

public record PaginationRequest(int Page = 1, int PageSize = 20, string? Search = null)
{
    public int Skip => (Page - 1) * PageSize;
}

public record PaginatedResponse<T>(
    IReadOnlyList<T> Items,
    int TotalCount,
    int Page,
    int PageSize,
    int TotalPages
);
