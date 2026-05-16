using GenZCoders.Models;

namespace GenZCoders.Services;

public interface ICourseRecommendationService
{
    Task<List<Course>> RecommendForUserAsync(Guid userId, CancellationToken ct = default);
}
