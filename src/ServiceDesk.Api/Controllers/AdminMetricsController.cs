using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Metrics;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/metrics")]
[Authorize(Policy = AuthPolicies.RequireAdministrador)]
public sealed class AdminMetricsController : ControllerBase
{
    private readonly IMetricsService _metricsService;

    public AdminMetricsController(IMetricsService metricsService)
    {
        _metricsService = metricsService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(AdminMetricsDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<AdminMetricsDto>> GetMetrics(
        [FromQuery] DateOnly? from,
        [FromQuery] DateOnly? to,
        [FromQuery] Guid? technicianId,
        [FromQuery] string? period,
        CancellationToken cancellationToken)
    {
        AdminMetricsDto metrics = await _metricsService.GetAdminMetricsAsync(
            from,
            to,
            technicianId,
            period,
            cancellationToken);

        return Ok(metrics);
    }
}
