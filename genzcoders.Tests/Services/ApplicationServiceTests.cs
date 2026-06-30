using GenZCoders.Data;
using GenZCoders.DTOs;
using GenZCoders.Models;
using GenZCoders.Models.Identity;
using GenZCoders.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace GenZCoders.Tests.Services;

public class ApplicationServiceTests
{
    private static AcademyDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<AcademyDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new AcademyDbContext(options);
    }

    [Fact]
    public async Task SubmitAsync_ThrowsWhenCourseNotFound()
    {
        var db = CreateInMemoryDb();
        var userManager = Mock.Of<UserManager<ApplicationUser>>(
            Mock.Of<IUserStore<ApplicationUser>>(), null!, null!, null!, null!, null!, null!, null!, null!);
        var notifications = Mock.Of<INotificationService>();

        var service = new ApplicationService(db, userManager, notifications);
        var request = new SubmitCourseApplicationRequest(
            CourseId: Guid.NewGuid(),
            CourseRoundId: Guid.NewGuid(),
            StudentEmail: "test@test.com",
            StudentName: "Test Student",
            PhoneNumber: null,
            SchoolName: null,
            GradeLevel: null,
            Answers: []);

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.SubmitAsync(request, CancellationToken.None));
    }

    [Fact]
    public async Task SubmitAsync_CreatesApplicationSuccessfully()
    {
        var db = CreateInMemoryDb();
        var courseId = Guid.NewGuid();
        db.Courses.Add(new Course
        {
            Id = courseId,
            Title = "Test Course",
            Slug = "test-course",
            Description = "Desc",
            ShortDescription = "Short",
            SkillsTaughtJson = "[]",
            Phase = 1,
            SortOrder = 1,
            IsActive = true,
        });
        await db.SaveChangesAsync();

        var userStore = new Mock<IUserStore<ApplicationUser>>();
        var userManager = new Mock<UserManager<ApplicationUser>>(
            userStore.Object, null!, null!, null!, null!, null!, null!, null!, null!);
        userManager.Setup(x => x.FindByEmailAsync(It.IsAny<string>()))
            .ReturnsAsync((ApplicationUser?)null);
        userManager.Setup(x => x.CreateAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);
        userManager.Setup(x => x.AddToRoleAsync(It.IsAny<ApplicationUser>(), It.IsAny<string>()))
            .ReturnsAsync(IdentityResult.Success);

        var notifications = Mock.Of<INotificationService>();

        var service = new ApplicationService(db, userManager.Object, notifications);
        var request = new SubmitCourseApplicationRequest(
            CourseId: courseId,
            CourseRoundId: null,
            StudentEmail: "new@test.com",
            StudentName: "New Student",
            PhoneNumber: null,
            SchoolName: null,
            GradeLevel: null,
            Answers: []);

        var result = await service.SubmitAsync(request, CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal("New Student", result.StudentName);
    }
}
