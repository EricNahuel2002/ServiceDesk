using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Domain.Tickets;

public class TicketAttachment : BaseEntity
{
    public Guid TicketId { get; set; }

    public Guid UploadedById { get; set; }

    public string FileName { get; set; } = string.Empty;

    public string BlobName { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeInBytes { get; set; }

    public Ticket? Ticket { get; set; }

    public ApplicationUser? UploadedBy { get; set; }
}
