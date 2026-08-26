using ServiceDesk.Application.DTOs.Audits;
using ServiceDesk.Application.DTOs.Chat;
using ServiceDesk.Application.DTOs.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IAuditService
{
    Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketDto>> GetTechnicianTicketsAsync(
        Guid technicianId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<TicketAuditEventDto>> GetTicketHistoryAsync(
        Guid ticketId,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ChatMessageDto>> GetTicketChatAsync(
        Guid ticketId,
        CancellationToken cancellationToken);
}
