using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/catalog")]
[Authorize]
public sealed class CatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public CatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryDto> categories = await _catalogService.GetCategoriesAsync(cancellationToken);

        return Ok(categories);
    }

    [HttpGet("priorities")]
    [ProducesResponseType(typeof(IReadOnlyList<PriorityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<PriorityDto>>> GetPriorities(CancellationToken cancellationToken)
    {
        IReadOnlyList<PriorityDto> priorities = await _catalogService.GetPrioritiesAsync(cancellationToken);

        return Ok(priorities);
    }

    [HttpGet("statuses")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<StatusDto>>> GetStatuses(CancellationToken cancellationToken)
    {
        IReadOnlyList<StatusDto> statuses = await _catalogService.GetStatusesAsync(cancellationToken);

        return Ok(statuses);
    }
}
