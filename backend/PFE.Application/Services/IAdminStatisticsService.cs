using PFE.Application.DTOs.Admin;

namespace PFE.Application.Services;

public interface IAdminStatisticsService
{
    Task<AdminStatisticsDto> GetStatisticsAsync(DateTime? from, DateTime? to, int? departmentId);
}
