using Microsoft.EntityFrameworkCore;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Company> Companies { get; }

    DbSet<Ticket> Tickets { get; }

    DbSet<TicketComment> TicketComments { get; }

    DbSet<TicketAttachment> TicketAttachments { get; }

    DbSet<Category> Categories { get; }

    DbSet<Priority> Priorities { get; }

    DbSet<Status> Statuses { get; }

    DbSet<AuditLog> AuditLogs { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
