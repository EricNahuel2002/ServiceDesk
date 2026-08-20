using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ITicketRepository
{
    Task<IReadOnlyList<TicketDto>> GetMineAsync(Guid createdById, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetAssignedToAsync(Guid assignedToId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketDto>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<TicketDto?> GetDtoByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    Task<Ticket?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default);

    Task<Ticket?> GetAssignedTicketByIdAsync(
        Guid id,
        Guid companyId,
        Guid assignedToId,
        CancellationToken cancellationToken = default);

    Task<TicketNotificationInfo?> GetTicketNotificationInfoAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default);

    Task<TicketAttachment?> GetAttachmentByIdAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetSlaTrackedTicketsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ticket>> GetAssignedUnstartedTicketsAsync(CancellationToken cancellationToken = default);

    Task<TicketSlaRecord?> GetCurrentSlaRecordAsync(Guid ticketId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsByTicketIdsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsByIdsAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketSlaRecord>> GetCanceledSlaRecordsPendingNotificationAsync(
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsPendingAdminReassignmentNotificationAsync(
        CancellationToken cancellationToken = default);

    void Add(Ticket ticket);

    void AddSlaRecord(TicketSlaRecord record);

    void AddComment(TicketComment comment);
}
