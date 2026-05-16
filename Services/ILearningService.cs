using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface ILearningService
{
    Task<Guid> AddLessonAsync(LessonCreateRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);
    Task<Guid> AddMaterialAsync(MaterialCreateRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);
    Task<Guid> CreateTaskAsync(TaskCreateRequest request, Guid? createdByUserId, CancellationToken cancellationToken = default);
    Task<Guid> SubmitTaskAsync(TaskSubmitRequest request, CancellationToken cancellationToken = default);
    Task GradeSubmissionAsync(Guid submissionId, Guid graderUserId, GradeSubmissionRequest request, CancellationToken cancellationToken = default);
    Task MarkAttendanceAsync(AttendanceMarkRequest request, Guid? markedByUserId, CancellationToken cancellationToken = default);
    Task<CourseRoomDto> GetRoomAsync(Guid courseId, Guid userId, CancellationToken cancellationToken = default);
    Task<StudentProgressDto> GetProgressAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentEnrollmentDto>> GetMyEnrollmentsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseMaterialDto2>> GetMyMaterialsAsync(Guid userId, Guid? courseId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentCertificateDto>> GetMyCertificatesAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<StudentSessionDto>> GetMySessionsAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CourseTaskDto>> GetMyTasksAsync(IReadOnlyList<Guid> cohortIds, Guid userId, CancellationToken cancellationToken = default);
}
