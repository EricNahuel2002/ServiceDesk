using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public class TicketFeedback : BaseEntity
{
    public Guid TicketId { get; set; }

    public Guid ClientId { get; set; }

    public bool WasSolved { get; set; }

    public int? Rating { get; set; }

    public string? Comment { get; set; }

    public Guid? TechnicianId { get; set; }

    public Ticket? Ticket { get; set; }

    public ApplicationUser? Client { get; set; }
}
