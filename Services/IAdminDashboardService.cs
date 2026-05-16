using GenZCoders.DTOs;

namespace GenZCoders.Services;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
