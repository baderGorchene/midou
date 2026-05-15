using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PFE.Application.Common;
using PFE.Application.DTOs.Admin;
using PFE.Application.Services;

namespace PFE.API.Controllers;

[ApiController]
[Route("api/admin/statistics")]
[Authorize(Roles = "Admin")]
public class AdminStatisticsController : ControllerBase
{
    private readonly IAdminStatisticsService _statisticsService;

    public AdminStatisticsController(IAdminStatisticsService statisticsService)
    {
        _statisticsService = statisticsService;
    }

    /// <summary>
    /// Aggregated statistics for admin dashboards. Optional department filters user-linked metrics.
    /// </summary>
    [HttpGet]
    public async Task<ActionResult<ApiResponse<AdminStatisticsDto>>> GetStatistics(
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to,
        [FromQuery] int? departmentId)
    {
        var dto = await _statisticsService.GetStatisticsAsync(from, to, departmentId);
        return Ok(ApiResponse<AdminStatisticsDto>.SuccessResponse(dto));
    }
}
