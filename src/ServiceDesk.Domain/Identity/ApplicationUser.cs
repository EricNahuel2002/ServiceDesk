using Microsoft.AspNetCore.Identity;
using ServiceDesk.Domain.Audit;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Tickets;

namespace ServiceDesk.Domain.Identity;

public class ApplicationUser : IdentityUser<Guid>
{
    public string FirstName { get; set; } = string.Empty;

    public string LastName { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public Company? Company { get; set; }

    public ICollection<Ticket> CreatedTickets { get; set; } = [];

    public ICollection<Ticket> AssignedTickets { get; set; } = [];

    public ICollection<TicketComment> Comments { get; set; } = [];

    public ICollection<TicketAttachment> UploadedAttachments { get; set; } = [];

    public ICollection<AuditLog> AuditLogs { get; set; } = [];
}
