using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public class TechnicianReport : BaseEntity
{
    public Guid TicketId { get; set; }

    public Guid ReportedById { get; set; }

    public Guid TechnicianId { get; set; }

    public string? Reason { get; set; }

    public Ticket? Ticket { get; set; }

    public ApplicationUser? ReportedBy { get; set; }

    public ApplicationUser? Technician { get; set; }

    public ICollection<TechnicianReportAttachment> Attachments { get; set; } = [];
}
