using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface IApplicationService
{
    Task<CourseApplicationDto> SubmitAsync(SubmitCourseApplicationRequest request, CancellationToken cancellationToken = default);
    Task<CourseApplicationDto> MarkPaidAsync(Guid applicationId, MarkApplicationPaidRequest request, CancellationToken cancellationToken = default);
    Task<CourseApplicationDto> ReviewAsync(Guid applicationId, Guid reviewerUserId, ApplicationReviewRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseApplicationDto>> PendingAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseApplicationDto>> GetByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}
