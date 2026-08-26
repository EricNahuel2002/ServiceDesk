using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Audits;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Api.Controllers;

[ApiController]
[Route("api/admin/audits")]
[Authorize(Policy = AuthPolicies.RequireAdministrador)]
public sealed class AdminAuditsController : ControllerBase
{
    private readonly IAuditService _auditService;

    public AdminAuditsController(IAuditService auditService)
    {
        _auditService = auditService;
    }

    [HttpGet("technicians")]
    [ProducesResponseType(typeof(IReadOnlyList<TechnicianDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TechnicianDto>>> GetTechnicians(
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TechnicianDto> technicians = await _auditService.GetTechniciansAsync(cancellationToken);
        return Ok(technicians);
    }

    [HttpGet("technicians/{technicianId:guid}/tickets")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TicketDto>>> GetTechnicianTickets(
        Guid technicianId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketDto> tickets = await _auditService.GetTechnicianTicketsAsync(technicianId, cancellationToken);
        return Ok(tickets);
    }

    [HttpGet("tickets/{ticketId:guid}/history")]
    [ProducesResponseType(typeof(IReadOnlyList<TicketAuditEventDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<TicketAuditEventDto>>> GetTicketHistory(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<TicketAuditEventDto> events = await _auditService.GetTicketHistoryAsync(ticketId, cancellationToken);
        return Ok(events);
    }

    [HttpGet("tickets/{ticketId:guid}/chat")]
    [ProducesResponseType(typeof(IReadOnlyList<ChatMessageDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
    public async Task<ActionResult<IReadOnlyList<ChatMessageDto>>> GetTicketChat(
        Guid ticketId,
        CancellationToken cancellationToken)
    {
        IReadOnlyList<ChatMessageDto> messages = await _auditService.GetTicketChatAsync(ticketId, cancellationToken);
        return Ok(messages);
    }
}
