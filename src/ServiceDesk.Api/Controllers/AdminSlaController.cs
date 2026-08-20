using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Sla;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/sla")]
[Authorize(Policy = AuthPolicies.RequireAdministrador)]
public sealed class AdminSlaController : ControllerBase
{
    private readonly ISlaService _slaService;

    public AdminSlaController(ISlaService slaService)
    {
        _slaService = slaService;
    }

    [HttpGet("configurations")]
    [ProducesResponseType(typeof(IReadOnlyList<SlaConfigurationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<SlaConfigurationDto>>> GetSlaConfigurations(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<SlaConfigurationDto> configurations = await _slaService.GetSlaConfigurationsAsync(cancellationToken);
        return Ok(configurations);
    }

    [HttpPut("configurations")]
    [ProducesResponseType(typeof(SlaConfigurationDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<SlaConfigurationDto>> UpdateSlaConfiguration(
        UpdateSlaConfigurationRequest request,
        CancellationToken cancellationToken)
    {
        SlaConfigurationDto configuration = await _slaService.UpdateSlaConfigurationAsync(request, cancellationToken);
        return Ok(configuration);
    }

    [HttpGet("business-hours")]
    [ProducesResponseType(typeof(BusinessHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BusinessHoursDto>> GetBusinessHours(CancellationToken cancellationToken)
    {
        BusinessHoursDto businessHours = await _slaService.GetBusinessHoursAsync(cancellationToken);
        return Ok(businessHours);
    }

    [HttpPut("business-hours")]
    [ProducesResponseType(typeof(BusinessHoursDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<BusinessHoursDto>> UpdateBusinessHours(
        UpdateBusinessHoursRequest request,
        CancellationToken cancellationToken)
    {
        BusinessHoursDto businessHours = await _slaService.UpdateBusinessHoursAsync(request, cancellationToken);
        return Ok(businessHours);
    }
}
