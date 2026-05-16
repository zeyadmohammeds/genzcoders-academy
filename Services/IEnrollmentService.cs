using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface IEnrollmentService
{
    Task<EnrollmentDto> CreateLeadAsync(EnrollmentRequest request, CancellationToken cancellationToken = default);
}
