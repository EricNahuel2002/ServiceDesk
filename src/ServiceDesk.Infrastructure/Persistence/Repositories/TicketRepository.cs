using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class TicketRepository : ITicketRepository
{
    private static readonly Expression<Func<Ticket, TicketDto>> TicketProjection = ticket => new TicketDto
    {
        Id = ticket.Id,
        Title = ticket.Title,
        Description = ticket.Description,
        CompanyId = ticket.CompanyId,
        CategoryId = ticket.CategoryId,
        CategoryName = ticket.Category!.Name,
        Priority = ticket.Priority,
        StatusId = ticket.StatusId,
        StatusName = ticket.Status!.Name,
        CreatedById = ticket.CreatedById,
        AssignedToId = ticket.AssignedToId,
        AssignedToFirstName = ticket.AssignedTo!.FirstName,
        AssignedToLastName = ticket.AssignedTo!.LastName,
        AssignedToEmail = ticket.AssignedTo!.Email,
        CreatedAtUtc = DateTime.SpecifyKind(ticket.CreatedAtUtc, DateTimeKind.Utc),
        UpdatedAtUtc = ticket.UpdatedAtUtc.HasValue
            ? DateTime.SpecifyKind(ticket.UpdatedAtUtc.Value, DateTimeKind.Utc)
            : null,
        AssignedAtUtc = ticket.AssignedAtUtc.HasValue
            ? DateTime.SpecifyKind(ticket.AssignedAtUtc.Value, DateTimeKind.Utc)
            : null,
        ResponseDeadlineAtUtc = DateTime.SpecifyKind(ticket.ResponseDeadlineAtUtc, DateTimeKind.Utc),
        StartedWorkAtUtc = ticket.StartedWorkAtUtc.HasValue
            ? DateTime.SpecifyKind(ticket.StartedWorkAtUtc.Value, DateTimeKind.Utc)
            : null,
        ResolvedAtUtc = ticket.ResolvedAtUtc.HasValue
            ? DateTime.SpecifyKind(ticket.ResolvedAtUtc.Value, DateTimeKind.Utc)
            : null,
        HasPendingFeedback = ticket.ResolvedAtUtc != null
            && ticket.Feedbacks.All(feedback => feedback.CreatedAtUtc < ticket.ResolvedAtUtc),
        CanReportTechnician = ticket.Feedbacks.Any(feedback => !feedback.WasSolved
            && ticket.TechnicianReports.All(report => report.CreatedAtUtc < feedback.CreatedAtUtc)),
        Attachments = ticket.Attachments
            .Select(attachment => new TicketAttachmentDto
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                ContentType = attachment.ContentType,
                SizeInBytes = attachment.SizeInBytes,
                BlobName = attachment.BlobName
            })
            .ToList()
    };

    private readonly ServiceDeskDbContext _context;

    public TicketRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<TicketDto>> GetMineAsync(Guid createdById, CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CreatedById == createdById)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(TicketProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetAssignedToAsync(
        Guid assignedToId,
        CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.AssignedToId == assignedToId)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(TicketProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketDto>> GetAllAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.CompanyId == companyId)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(TicketProjection)
            .ToListAsync(cancellationToken);

    public async Task<TicketDto?> GetDtoByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == id && ticket.CompanyId == companyId)
            .Select(TicketProjection)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Ticket?> GetByIdAsync(Guid id, Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .Where(ticket => ticket.Id == id && ticket.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Ticket?> GetAssignedTicketByIdAsync(
        Guid id,
        Guid companyId,
        Guid assignedToId,
        CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .Where(ticket => ticket.Id == id
                && ticket.CompanyId == companyId
                && ticket.AssignedToId == assignedToId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<Ticket?> GetClientTicketByIdAsync(
        Guid id,
        Guid companyId,
        Guid clientId,
        CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .Where(ticket => ticket.Id == id
                && ticket.CompanyId == companyId
                && ticket.CreatedById == clientId)
            .Include(ticket => ticket.Feedbacks)
            .Include(ticket => ticket.TechnicianReports)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TicketNotificationInfo?> GetTicketNotificationInfoAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.Id == ticketId && ticket.AssignedToId != null)
            .Select(ticket => new TicketNotificationInfo
            {
                TicketId = ticket.Id,
                Title = ticket.Title,
                Description = ticket.Description,
                PriorityName = ticket.Priority != null ? ticket.Priority.ToString()! : "Sin asignar",
                AssignedToFirstName = ticket.AssignedTo!.FirstName,
                AssignedToLastName = ticket.AssignedTo!.LastName,
                AssignedToEmail = ticket.AssignedTo!.Email ?? string.Empty,
                RequesterFirstName = ticket.CreatedBy!.FirstName,
                RequesterEmail = ticket.CreatedBy!.Email ?? string.Empty
            })
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<TicketAttachment?> GetAttachmentByIdAsync(
        Guid ticketId,
        Guid attachmentId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.TicketAttachments
            .AsNoTracking()
            .Where(attachment => attachment.Id == attachmentId
                && attachment.TicketId == ticketId
                && attachment.Ticket!.CompanyId == companyId)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<Ticket>> GetSlaTrackedTicketsAsync(CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .Where(ticket => ticket.ResolvedAtUtc == null
                && ticket.AssignedToId != null
                && ticket.Priority != null
                && ticket.SlaRecords.Any(record => record.IsCurrent && record.CanceledAtUtc == null))
            .Include(ticket => ticket.SlaRecords)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<Ticket>> GetAssignedUnstartedTicketsAsync(CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .Where(ticket => ticket.ResolvedAtUtc == null
                && ticket.AssignedToId != null
                && ticket.AssignedAtUtc != null
                && ticket.StartedWorkAtUtc == null
                && ticket.Priority != null
                && ticket.SlaRecords.Any(record => record.IsCurrent && record.CanceledAtUtc == null))
            .Include(ticket => ticket.SlaRecords)
            .ToListAsync(cancellationToken);

    public async Task<TicketSlaRecord?> GetCurrentSlaRecordAsync(
        Guid ticketId,
        CancellationToken cancellationToken = default) =>
        await _context.TicketSlaRecords
            .Where(record => record.TicketId == ticketId && record.IsCurrent && record.CanceledAtUtc == null)
            .SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsByTicketIdsAsync(
        IReadOnlyCollection<Guid> ticketIds,
        CancellationToken cancellationToken = default) =>
        await _context.TicketSlaRecords
            .Where(record => ticketIds.Contains(record.TicketId))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsByIdsAsync(
        IReadOnlyCollection<Guid> recordIds,
        CancellationToken cancellationToken = default) =>
        await _context.TicketSlaRecords
            .Where(record => recordIds.Contains(record.Id))
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketSlaRecord>> GetCanceledSlaRecordsPendingNotificationAsync(
        CancellationToken cancellationToken = default) =>
        await _context.TicketSlaRecords
            .Where(record => record.CanceledAtUtc != null
                && record.CanceledNotifiedAtUtc == null
                && record.TechnicianId != null
                && record.CanceledReason != SlaRecordCancelReason.AssignmentStartGraceExceeded
                && record.CanceledReason != SlaRecordCancelReason.ReopenedByClientFeedback)
            .Include(record => record.Ticket)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<TicketSlaRecord>> GetSlaRecordsPendingAdminReassignmentNotificationAsync(
        CancellationToken cancellationToken = default) =>
        await _context.TicketSlaRecords
            .Where(record => record.CanceledAtUtc != null
                && record.AdminReassignmentNotifiedAtUtc == null
                && (record.CanceledReason == SlaRecordCancelReason.AssignmentStartGraceExceeded
                    || record.CanceledReason == SlaRecordCancelReason.ReopenedByClientFeedback))
            .Include(record => record.Ticket)
            .ToListAsync(cancellationToken);

    public void Add(Ticket ticket) => _context.Tickets.Add(ticket);

    public void AddSlaRecord(TicketSlaRecord record) => _context.TicketSlaRecords.Add(record);

    public void AddComment(TicketComment comment) => _context.TicketComments.Add(comment);

    public void AddFeedback(TicketFeedback feedback) => _context.TicketFeedbacks.Add(feedback);

    public void AddTechnicianReport(TechnicianReport report) => _context.TechnicianReports.Add(report);

    public async Task<IReadOnlyList<TicketDto>> GetAssignedToInCompanyAsync(
        Guid assignedToId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.Tickets
            .AsNoTracking()
            .Where(ticket => ticket.AssignedToId == assignedToId && ticket.CompanyId == companyId)
            .OrderByDescending(ticket => ticket.CreatedAtUtc)
            .Select(TicketProjection)
            .ToListAsync(cancellationToken);

    public async Task<IReadOnlyList<AuditLog>> GetTicketAuditLogsAsync(
        Guid ticketId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await _context.AuditLogs
            .AsNoTracking()
            .Where(log => log.EntityId == ticketId && log.EntityType == "Ticket" && log.CompanyId == companyId)
            .Include(log => log.User)
            .OrderBy(log => log.CreatedAtUtc)
            .ToListAsync(cancellationToken);

    public void AddAuditLog(AuditLog auditLog) => _context.AuditLogs.Add(auditLog);
}
