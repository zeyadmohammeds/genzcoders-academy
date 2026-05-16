namespace GenZCoders.DTOs;

public record EnrollmentRequest(Guid CourseId, string StudentEmail, string StudentName, string? PromoCode);

public record EnrollmentDto(Guid Id, Guid CourseId, string CourseTitle, string StudentEmail, string Status, decimal FinalPriceEgp);
