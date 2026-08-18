using ServiceDesk.Domain.Enums;

namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record UpdateTicketRequest
{
    public Guid AssignedToId { get; init; }

    public TicketPriority Priority { get; init; }

    public Guid StatusId { get; init; }
}
