using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Repos;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class CourseService(IRepository<Course> courses) : ICourseService
{
    public async Task<IReadOnlyList<CourseDto>> GetFeaturedAsync(CancellationToken cancellationToken = default)
    {
        var featured = await courses.Query()
            .AsNoTracking()
            .Where(x => x.IsFeatured)
            .Include(x => x.Modules.OrderBy(m => m.SortOrder))
            .OrderBy(x => x.PriceEgp)
            .ToListAsync(cancellationToken);

        return featured.Select(ToDto).ToList();
    }

    public async Task<CourseDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var course = await courses.Query()
            .AsNoTracking()
            .Include(x => x.Modules.OrderBy(m => m.SortOrder))
            .Where(x => x.Slug == slug)
            .FirstOrDefaultAsync(cancellationToken);

        return course is null ? null : ToDto(course);
    }

    private static CourseDto ToDto(Course course) => new(
        course.Id,
        course.Slug,
        course.Title,
        course.ShortDescription,
        course.Outcome,
        course.MinimumAge,
        course.PriceEgp,
        course.CoreSessions,
        course.SupportSessions,
        course.Level,
        course.Modules.OrderBy(x => x.SortOrder)
            .Select(x => new CourseModuleDto(x.SortOrder, x.Title, x.ProjectOutcome))
            .ToList());
}
