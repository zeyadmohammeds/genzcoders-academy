using GenZCoders.Models;

namespace GenZCoders.DTOs;

public record CourseRoundCreateRequest(
    Guid CourseId,
    string Name,
    string Slug,
    string? Description,
    DateOnly StartDate,
    DateOnly? EndDate,
    int MaxStudents,
    Guid? EngineerUserId,
    Guid? CtaUserId,
    CourseRoundMode Mode,
    bool AutoAcceptPaidApplications,
    bool RequireEngineerApproval);

public record CourseRoundDto(
    Guid Id,
    Guid CourseId,
    string CourseTitle,
    string Name,
    string Slug,
    CohortStatus Status,
    DateOnly StartDate,
    DateOnly? EndDate,
    int MaxStudents,
    int CurrentStudents,
    bool IsEnrollmentOpen,
    bool AutoAcceptPaidApplications,
    bool RequireEngineerApproval);

public record MoveStudentRoundRequest(Guid StudentUserId, Guid FromCourseRoundId, Guid ToCourseRoundId);
