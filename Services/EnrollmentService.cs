using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Services;

public class EnrollmentService(
    AcademyDbContext db,
    UserManager<ApplicationUser> userManager) : IEnrollmentService
{
    public async Task<EnrollmentDto> CreateLeadAsync(EnrollmentRequest request, CancellationToken cancellationToken = default)
    {
        var course = await db.Courses.FirstOrDefaultAsync(x => x.Id == request.CourseId, cancellationToken)
            ?? throw new InvalidOperationException("Course was not found.");

        var normalizedEmail = request.StudentEmail.Trim().ToLowerInvariant();
        var user = await userManager.FindByEmailAsync(normalizedEmail);

        if (user is null)
        {
            var names = request.StudentName.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            user = new ApplicationUser
            {
                UserName = normalizedEmail,
                Email = normalizedEmail,
                FirstName = names.FirstOrDefault() ?? "Student",
                LastName = names.Length > 1 ? names[1] : string.Empty,
                RoleKey = AcademyRole.Student,
                EmailConfirmed = false
            };

            var result = await userManager.CreateAsync(user);
            if (!result.Succeeded)
            {
                throw new InvalidOperationException(string.Join("; ", result.Errors.Select(x => x.Description)));
            }

            await userManager.AddToRoleAsync(user, AcademyRole.Student);
        }

        var existing = await db.Enrollments
            .Include(x => x.Course)
            .FirstOrDefaultAsync(x => x.CourseId == course.Id && x.StudentUserId == user.Id, cancellationToken);

        if (existing is not null)
        {
            return new EnrollmentDto(existing.Id, course.Id, course.Title, normalizedEmail, existing.Status, existing.FinalPriceEgp);
        }

        var discount = string.Equals(request.PromoCode, "PARTNER15", StringComparison.OrdinalIgnoreCase) ? 0.15m : 0m;
        var enrollment = new Enrollment
        {
            CourseId = course.Id,
            StudentUserId = user.Id,
            PromoCode = request.PromoCode,
            UnitPriceEgp = course.PriceEgp,
            DiscountAmountEgp = course.PriceEgp * discount,
            FinalPriceEgp = course.PriceEgp * (1 - discount)
        };

        db.Enrollments.Add(enrollment);
        await db.SaveChangesAsync(cancellationToken);

        return new EnrollmentDto(enrollment.Id, course.Id, course.Title, normalizedEmail, enrollment.Status, enrollment.FinalPriceEgp);
    }
}
