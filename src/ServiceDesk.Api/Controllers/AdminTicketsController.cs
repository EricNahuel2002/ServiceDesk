using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/tickets")]
[Authorize(Policy = AuthPolicies.RequireAdministrador)]
public sealed class AdminTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public AdminTicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetAll(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _ticketService.GetAllAsync(cancellationToken);

        return Ok(tickets);
    }

    [HttpGet("technicians")]
    [ProducesResponseType(typeof(IReadOnlyList<TechnicianDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TechnicianDto>>> GetTechnicians(CancellationToken cancellationToken)
    {
        IReadOnlyList<TechnicianDto> technicians = await _ticketService.GetTechniciansAsync(cancellationToken);

        return Ok(technicians);
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketDto>> GetById(Guid id, CancellationToken cancellationToken)
    {
        TicketDto ticket = await _ticketService.GetByIdAsync(id, cancellationToken);

        return Ok(ticket);
    }

    [HttpPatch("{id:guid}")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketDto>> Update(
        Guid id,
        UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        TicketDto ticket = await _ticketService.UpdateAsync(id, request, cancellationToken);

        return Ok(ticket);
    }
}
