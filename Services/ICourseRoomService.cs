using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface ICourseRoomService
{
    Task<CourseRoomDto> GetRoomAsync(Guid studentUserId, Guid courseRoundId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<LeaderboardEntryDto>> LeaderboardAsync(Guid? courseRoundId = null, Guid? courseId = null, CancellationToken cancellationToken = default);
}
