using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public class TicketComment : BaseEntity
{
    public Guid TicketId { get; set; }

    public Guid? AuthorId { get; set; }

    public string Body { get; set; } = string.Empty;

    public bool IsInternal { get; set; }

    public Ticket? Ticket { get; set; }

    public ApplicationUser? Author { get; set; }
}
