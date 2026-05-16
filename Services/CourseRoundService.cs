using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class CourseRoundService(AcademyDbContext db) : ICourseRoundService
{
    public async Task<CourseRoundDto> CreateAsync(CourseRoundCreateRequest request, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course not found.");

        var round = new Cohort
        {
            CourseId = request.CourseId,
            Name = request.Name,
            Slug = request.Slug,
            Description = request.Description,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            MaxStudents = request.MaxStudents,
            EngineerUserId = request.EngineerUserId,
            CtaUserId = request.CtaUserId,
            Mode = request.Mode,
            AutoAcceptPaidApplications = request.AutoAcceptPaidApplications,
            RequireEngineerApproval = request.RequireEngineerApproval,
            Status = CohortStatus.Upcoming
        };

        db.Cohorts.Add(round);
        await db.SaveChangesAsync(cancellationToken);
        return ToDto(round, course.Title);
    }

    public async Task<IReadOnlyList<CourseRoundDto>> ListAsync(Guid? courseId = null, CancellationToken cancellationToken = default)
        => await db.Cohorts
            .AsNoTracking()
            .Include(x => x.Course)
            .Include(x => x.CohortEnrollments)
            .Where(x => courseId == null || x.CourseId == courseId)
            .OrderByDescending(x => x.StartDate)
            .Select(x => ToDto(x, x.Course == null ? string.Empty : x.Course.Title))
            .ToListAsync(cancellationToken);

    public async Task MoveStudentAsync(MoveStudentRoundRequest request, CancellationToken cancellationToken = default)
    {
        var existing = await db.CohortEnrollments.FirstOrDefaultAsync(x => x.CohortId == request.FromCourseRoundId && x.StudentUserId == request.StudentUserId, cancellationToken);
        if (existing is not null)
        {
            db.CohortEnrollments.Remove(existing);
        }

        if (!await db.CohortEnrollments.AnyAsync(x => x.CohortId == request.ToCourseRoundId && x.StudentUserId == request.StudentUserId, cancellationToken))
        {
            db.CohortEnrollments.Add(new CohortEnrollment
            {
                CohortId = request.ToCourseRoundId,
                StudentUserId = request.StudentUserId
            });
        }

        await db.Enrollments
            .Where(x => x.CohortId == request.FromCourseRoundId && x.StudentUserId == request.StudentUserId)
            .ExecuteUpdateAsync(setters => setters.SetProperty(x => x.CohortId, request.ToCourseRoundId), cancellationToken);

        await db.SaveChangesAsync(cancellationToken);
    }

    private static CourseRoundDto ToDto(Cohort round, string courseTitle)
        => new(round.Id, round.CourseId, courseTitle, round.Name, round.Slug, round.Status, round.StartDate, round.EndDate, round.MaxStudents, round.CohortEnrollments.Count, round.IsEnrollmentOpen, round.AutoAcceptPaidApplications, round.RequireEngineerApproval);
}
