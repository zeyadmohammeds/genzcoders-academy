using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using GenZCoders.Services;
using Microsoft.EntityFrameworkCore;

namespace GenZCoders.Tests.Services;

public class CourseRoomServiceTests
{
    private static AcademyDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AcademyDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new AcademyDbContext(options);
    }

    [Fact]
    public async Task GetRoomAsync_ThrowsWhenRoundNotFound()
    {
        var db = CreateInMemoryDb();
        var service = new CourseRoomService(db);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.GetRoomAsync(Guid.NewGuid(), Guid.NewGuid(), CancellationToken.None));
    }

    [Fact]
    public async Task GetRoomAsync_ReturnsCorrectStructure()
    {
        var db = CreateInMemoryDb();
        var courseId = Guid.NewGuid();
        var roundId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        db.Courses.Add(new Course
        {
            Id = courseId,
            Title = "Robotics 101",
            Slug = "robotics-101",
            Description = "Build robots",
            ShortDescription = "Robots",
            SkillsTaughtJson = "[]",
            Phase = 1,
            SortOrder = 1,
            IsActive = true,
        });

        db.Cohorts.Add(new Cohort
        {
            Id = roundId,
            CourseId = courseId,
            Name = "Summer 2026",
            Status = CohortStatus.Active,
            StartDate = DateTimeOffset.UtcNow,
            EndDate = DateTimeOffset.UtcNow.AddMonths(3),
        });

        db.Users.Add(new ApplicationUser
        {
            Id = userId,
            UserName = "student@test.com",
            Email = "student@test.com",
            RoleKey = AcademyRole.Student,
        });

        db.CohortEnrollments.Add(new CohortEnrollment
        {
            CohortId = roundId,
            StudentUserId = userId,
            EnrolledAt = DateTimeOffset.UtcNow,
        });

        db.XpTransactions.Add(new XpTransaction
        {
            StudentUserId = userId,
            Amount = 500,
            Reason = "Test",
            CreatedAt = DateTimeOffset.UtcNow,
        });

        await db.SaveChangesAsync();

        var service = new CourseRoomService(db);
        var result = await service.GetRoomAsync(userId, roundId, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(courseId, result.CourseId);
        Assert.Equal(roundId, result.CourseRoundId);
        Assert.Equal("Robotics 101", result.CourseTitle);
        Assert.Equal("Summer 2026", result.RoundName);
        Assert.Equal(CourseAccessStatus.Open, result.AccessStatus);
        Assert.Equal(500, result.Progress.XpTotal);
        Assert.Empty(result.Weeks);
        Assert.Empty(result.Tasks);
        Assert.Empty(result.Quizzes);
    }
}
