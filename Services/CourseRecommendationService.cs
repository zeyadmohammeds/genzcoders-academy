using GenZCoders.Data;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace GenZCoders.Services;

public class CourseRecommendationService(AcademyDbContext db) : ICourseRecommendationService
{
    public async Task<List<Course>> RecommendForUserAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await db.StudentProfiles.FirstOrDefaultAsync(x => x.UserId == userId, ct);
        
        var enrolledCourseIds = await db.Enrollments
            .Where(x => x.StudentUserId == userId)
            .Select(x => x.CourseId)
            .ToListAsync(ct);

        var allCourses = await db.Courses.Where(x => x.IsActive).ToListAsync(ct);
        var candidates = allCourses.Where(c => !enrolledCourseIds.Contains(c.Id)).ToList();

        if (profile == null || string.IsNullOrEmpty(profile.InterestsJson))
        {
            return candidates.Take(3).ToList();
        }

        try 
        {
            var interests = JsonSerializer.Deserialize<List<string>>(profile.InterestsJson) ?? new List<string>();
            
            return candidates
                .OrderByDescending(c => interests.Count(i => 
                    c.Title.Contains(i, StringComparison.OrdinalIgnoreCase) || 
                    c.Description.Contains(i, StringComparison.OrdinalIgnoreCase)))
                .ThenBy(c => Guid.NewGuid()) // Randomize tied results
                .Take(3)
                .ToList();
        }
        catch
        {
            return candidates.Take(3).ToList();
        }
    }
}
