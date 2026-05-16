using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class AdminDashboardService(AcademyDbContext db) : IAdminDashboardService
{
    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var enrollments = await db.Enrollments
            .AsNoTracking()
            .Include(x => x.Course)
            .ToListAsync(cancellationToken);

        var courseDemand = enrollments
            .GroupBy(x => new { x.CourseId, CourseTitle = x.Course?.Title ?? "Unknown course" })
            .Select(x => new CourseDemandDto(x.Key.CourseId, x.Key.CourseTitle, x.Count(), x.Sum(e => e.FinalPriceEgp)))
            .OrderByDescending(x => x.EnrollmentCount)
            .Take(10)
            .ToList();

        var absentRecords = await db.AttendanceRecords
            .AsNoTracking()
            .Include(x => x.StudentUser)
            .Where(x => x.Status == AttendanceStatus.Absent || x.Status == AttendanceStatus.Late)
            .ToListAsync(cancellationToken);

        var atRiskStudents = absentRecords
            .GroupBy(x => new { x.StudentUserId, Name = x.StudentUser != null ? x.StudentUser.FirstName + " " + x.StudentUser.LastName : "Unknown", Email = x.StudentUser?.Email ?? string.Empty })
            .Select(x => new AtRiskStudentDto(x.Key.StudentUserId, x.Key.Name, x.Key.Email, x.Count(), 0))
            .OrderByDescending(x => x.MissedSessions)
            .Take(10)
            .ToList();

        return new AdminDashboardDto(
            TotalCourses: await db.Courses.CountAsync(cancellationToken),
            ActiveCourses: await db.Courses.CountAsync(x => x.IsActive, cancellationToken),
            TotalSchools: await db.Schools.CountAsync(cancellationToken),
            PartnerSchools: await db.Schools.CountAsync(x => x.PartnershipStatus == PartnershipStatus.Active || x.PartnershipStatus == PartnershipStatus.FoundingPartner, cancellationToken),
            TotalEnrollments: enrollments.Count,
            PaidOrders: await db.EnrollmentOrders.CountAsync(x => x.PaymentStatus == PaymentStatus.Paid, cancellationToken),
            RevenueEgp: await db.EnrollmentOrders.Where(x => x.PaymentStatus == PaymentStatus.Paid).SumAsync(x => x.TotalAmountEgp, cancellationToken),
            UpcomingSessions: await db.SessionInstances.CountAsync(x => x.ScheduledAt >= DateTimeOffset.UtcNow && x.Status == SessionStatus.Scheduled, cancellationToken),
            PendingSubmissions: await db.TaskSubmissions.CountAsync(x => x.Status == SubmissionStatus.Pending, cancellationToken),
            OpenStudentQuestions: await db.StudentQuestions.CountAsync(x => x.Status == "open", cancellationToken),
            CourseDemand: courseDemand,
            AtRiskStudents: atRiskStudents);
    }
}
