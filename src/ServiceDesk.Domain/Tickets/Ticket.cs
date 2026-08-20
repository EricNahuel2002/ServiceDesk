using ServiceDesk.Domain.Catalog;
using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Companies;
using ServiceDesk.Domain.Enums;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public class Ticket : BaseEntity
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public Guid CompanyId { get; set; }

    public Guid CategoryId { get; set; }

    public TicketPriority? Priority { get; set; }

    public Guid StatusId { get; set; }

    public Guid CreatedById { get; set; }

    public Guid? AssignedToId { get; set; }

    public DateTime ResponseDeadlineAtUtc { get; set; }

    public DateTime? StartedWorkAtUtc { get; set; }

    public DateTime? AssignedAtUtc { get; set; }

    public DateTime? ResolvedAtUtc { get; set; }

    public Company? Company { get; set; }

    public Category? Category { get; set; }

    public Status? Status { get; set; }

    public ApplicationUser? CreatedBy { get; set; }

    public ApplicationUser? AssignedTo { get; set; }

    public ICollection<TicketComment> Comments { get; set; } = [];

    public ICollection<TicketAttachment> Attachments { get; set; } = [];

    public ICollection<TicketSlaRecord> SlaRecords { get; set; } = [];
}
