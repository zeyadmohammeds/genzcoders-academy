namespace GenZCoders.DTOs;

public record CourseDto(
    Guid Id,
    string Slug,
    string Title,
    string ShortDescription,
    string Outcome,
    int MinimumAge,
    decimal PriceEgp,
    int CoreSessions,
    int SupportSessions,
    string Level,
    string? CoverImageUrl,
    string? ImageUrl,
    IReadOnlyList<CourseModuleDto> Modules);

public record CourseModuleDto(int SortOrder, string Title, string ProjectOutcome);
