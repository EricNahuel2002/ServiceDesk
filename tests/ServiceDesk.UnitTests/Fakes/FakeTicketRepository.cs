using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeTicketRepository : ITicketRepository
{
    public TicketNotificationInfo? NotificationInfo { get; set; }

    public Task<TicketNotificationInfo?> GetTicketNotificationInfoAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(NotificationInfo);

    public Task<IReadOnlyList<TicketDto>> GetMineAsync(
        Guid createdById,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<TicketDto>> GetAssignedToAsync(
        Guid assignedToId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<TicketDto>> GetAllAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TicketDto?> GetDtoByIdAsync(
        Guid id,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Ticket?> GetByIdAsync(
        Guid id,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<Ticket?> GetAssignedTicketByIdAsync(
        Guid id,
        Guid companyId,
        Guid assignedToId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TicketAttachment?> GetAttachmentByIdAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public void Add(Ticket ticket) => throw new NotSupportedException();

    public void AddComment(TicketComment comment) => throw new NotSupportedException();
}
