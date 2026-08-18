using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public class ChatMessage : BaseEntity
{
    public Guid TicketId { get; set; }

    public Guid SenderId { get; set; }

    public string Content { get; set; } = string.Empty;

    public DateTime SentAtUtc { get; set; }

    public DateTime? ReadAtUtc { get; set; }

    public Ticket? Ticket { get; set; }

    public ApplicationUser? Sender { get; set; }
}
