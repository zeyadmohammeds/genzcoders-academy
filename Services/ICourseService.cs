using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface ICourseService
{
    Task<IReadOnlyList<CourseDto>> GetFeaturedAsync(CancellationToken cancellationToken = default);
    Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
