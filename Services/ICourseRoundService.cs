using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface ICourseRoundService
{
    Task<CourseRoundDto> CreateAsync(CourseRoundCreateRequest request, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseRoundDto>> ListAsync(Guid? courseId = null, CancellationToken cancellationToken = default);
    Task MoveStudentAsync(MoveStudentRoundRequest request, CancellationToken cancellationToken = default);
}
