using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Notifications;
using ServiceDesk.Application.DTOs.Tickets;
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
        CreatedAtUtc = ticket.CreatedAtUtc,
        UpdatedAtUtc = ticket.UpdatedAtUtc,
        ResponseDeadlineAtUtc = ticket.ResponseDeadlineAtUtc,
        StartedWorkAtUtc = ticket.StartedWorkAtUtc,
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
                PriorityName = ticket.Priority.ToString(),
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

    public void Add(Ticket ticket) => _context.Tickets.Add(ticket);

    public void AddComment(TicketComment comment) => _context.TicketComments.Add(comment);
}
