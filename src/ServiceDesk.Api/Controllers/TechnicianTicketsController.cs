using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/technician/tickets")]
[Authorize(Policy = AuthPolicies.RequireTecnico)]
public sealed class TechnicianTicketsController : ControllerBase
{
    private readonly ITicketService _ticketService;

    public TechnicianTicketsController(ITicketService ticketService)
    {
        _ticketService = ticketService;
    }

    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetAssignedToMe(CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _ticketService.GetAssignedToMeAsync(cancellationToken);

        return Ok(tickets);
    }

    [HttpPatch("{id:guid}/start-work")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketDto>> StartWork(
        Guid id,
        CancellationToken cancellationToken)
    {
        TicketDto ticket = await _ticketService.StartWorkAsync(id, cancellationToken);

        return Ok(ticket);
    }

    [HttpPatch("{id:guid}/resolve")]
    [ProducesResponseType(typeof(TicketDto), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ValidationProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<TicketDto>> Resolve(
        Guid id,
        ResolveTicketRequest request,
        CancellationToken cancellationToken)
    {
        TicketDto ticket = await _ticketService.ResolveAsync(id, request, cancellationToken);

        return Ok(ticket);
    }
}
