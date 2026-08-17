using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Catalog;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/catalog")]
[Authorize(Policy = AuthPolicies.RequireAdministrador)]
public sealed class AdminCatalogController : ControllerBase
{
    private readonly ICatalogService _catalogService;

    public AdminCatalogController(ICatalogService catalogService)
    {
        _catalogService = catalogService;
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<CategoryDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<CategoryDto>>> GetCategories(CancellationToken cancellationToken)
    {
        IReadOnlyList<CategoryDto> categories = await _catalogService.GetAllCategoriesAsync(cancellationToken);
        return Ok(categories);
    }

    [HttpPost("categories")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<CategoryDto>> CreateCategory(
        CreateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        CategoryDto category = await _catalogService.CreateCategoryAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetCategories), new { }, category);
    }

    [HttpPut("categories/{id:guid}")]
    [ProducesResponseType(typeof(CategoryDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<CategoryDto>> UpdateCategory(
        Guid id,
        UpdateCategoryRequest request,
        CancellationToken cancellationToken)
    {
        CategoryDto category = await _catalogService.UpdateCategoryAsync(id, request, cancellationToken);
        return Ok(category);
    }

    [HttpGet("priorities")]
    [ProducesResponseType(typeof(IReadOnlyList<PriorityDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<PriorityDto>>> GetPriorities(CancellationToken cancellationToken)
    {
        IReadOnlyList<PriorityDto> priorities = await _catalogService.GetAllPrioritiesAsync(cancellationToken);
        return Ok(priorities);
    }

    [HttpPost("priorities")]
    [ProducesResponseType(typeof(PriorityDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<PriorityDto>> CreatePriority(
        CreatePriorityRequest request,
        CancellationToken cancellationToken)
    {
        PriorityDto priority = await _catalogService.CreatePriorityAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetPriorities), new { }, priority);
    }

    [HttpPut("priorities/{id:guid}")]
    [ProducesResponseType(typeof(PriorityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<PriorityDto>> UpdatePriority(
        Guid id,
        UpdatePriorityRequest request,
        CancellationToken cancellationToken)
    {
        PriorityDto priority = await _catalogService.UpdatePriorityAsync(id, request, cancellationToken);
        return Ok(priority);
    }

    [HttpGet("statuses")]
    [ProducesResponseType(typeof(IReadOnlyList<StatusDto>), StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<StatusDto>>> GetStatuses(CancellationToken cancellationToken)
    {
        IReadOnlyList<StatusDto> statuses = await _catalogService.GetAllStatusesAsync(cancellationToken);
        return Ok(statuses);
    }

    [HttpPost("statuses")]
    [ProducesResponseType(typeof(StatusDto), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<StatusDto>> CreateStatus(
        CreateStatusRequest request,
        CancellationToken cancellationToken)
    {
        StatusDto status = await _catalogService.CreateStatusAsync(request, cancellationToken);
        return CreatedAtAction(nameof(GetStatuses), new { }, status);
    }

    [HttpPut("statuses/{id:guid}")]
    [ProducesResponseType(typeof(StatusDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<StatusDto>> UpdateStatus(
        Guid id,
        UpdateStatusRequest request,
        CancellationToken cancellationToken)
    {
        StatusDto status = await _catalogService.UpdateStatusAsync(id, request, cancellationToken);
        return Ok(status);
    }
}
