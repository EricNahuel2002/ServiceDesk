using ServiceDesk.Domain.Common;
using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Domain.Tickets;

public class TicketSlaRecord : BaseEntity
{
    public Guid TicketId { get; set; }

    public Guid? TechnicianId { get; set; }

    public TicketPriority Priority { get; set; }

    public int SlaLimitHours { get; set; }

    public DateTime ResponseDeadlineAtUtc { get; set; }

    public DateTime? BreachedAtUtc { get; set; }

    public DateTime? GraceDeadlineUtc { get; set; }

    public DateTime? CanceledAtUtc { get; set; }

    public SlaRecordCancelReason? CanceledReason { get; set; }

    public DateTime? AdminReassignmentNotifiedAtUtc { get; set; }

    public DateTime? ExpiringNotifiedAtUtc { get; set; }

    public DateTime? BreachedNotifiedAtUtc { get; set; }

    public DateTime? CanceledNotifiedAtUtc { get; set; }

    public bool IsCurrent { get; set; }

    public Ticket? Ticket { get; set; }
}