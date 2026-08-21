using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeTicketRepository : ITicketRepository
{
    public TicketNotificationInfo? NotificationInfo { get; set; }

    public List<Ticket> SlaTickets { get; } = [];

    public List<TicketSlaRecord> SlaRecords { get; } = [];

    public List<TicketComment> Comments { get; } = [];

    public Task<TicketNotificationInfo?> GetTicketNotificationInfoAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(NotificationInfo);

    public Task<IReadOnlyList<Ticket>> GetSlaTrackedTicketsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Ticket>>(SlaTickets);

    public Task<IReadOnlyList<Ticket>> GetAssignedUnstartedTicketsAsync(CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Ticket>>(SlaTickets);

    public Task<TicketSlaRecord?> GetCurrentSlaRecordAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SlaRecords.FirstOrDefault(record =>
            record.TicketId == ticketId && record.IsCurrent && record.CanceledAtUtc == null));

    public Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsByTicketIdsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TicketSlaRecord>>(
            SlaRecords.Where(record => ticketIds.Contains(record.TicketId)).ToList());

    public Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsByIdsAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TicketSlaRecord>>(
            SlaRecords.Where(record => recordIds.Contains(record.Id)).ToList());

    public Task<IReadOnlyList<TicketSlaRecord>> GetCanceledSlaRecordsPendingNotificationAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TicketSlaRecord>>(SlaRecords
            .Where(record => record.CanceledAtUtc != null
                && record.CanceledNotifiedAtUtc == null
                && record.TechnicianId != null)
            .ToList());

    public Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsPendingAdminReassignmentNotificationAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<TicketSlaRecord>>(SlaRecords
            .Where(record => record.CanceledAtUtc != null
                && record.AdminReassignmentNotifiedAtUtc == null
                && (record.CanceledReason == SlaRecordCancelReason.AssignmentStartGraceExceeded
                    || record.CanceledReason == SlaRecordCancelReason.ReopenedByClientFeedback))
            .ToList());

    public void Add(Ticket ticket) => throw new NotSupportedException();

    public void AddSlaRecord(TicketSlaRecord record) => SlaRecords.Add(record);

    public void AddComment(TicketComment comment) => Comments.Add(comment);

    public void AddFeedback(TicketFeedback feedback) => throw new NotSupportedException();

    public void AddTechnicianReport(TechnicianReport report) => throw new NotSupportedException();

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

    public Task<Ticket?> GetClientTicketByIdAsync(
        Guid id,
        Guid companyId,
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<TicketAttachment?> GetAttachmentByIdAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}