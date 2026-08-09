namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record UpdateTicketRequest
{
    public Guid AssignedToId { get; init; }

    public Guid PriorityId { get; init; }

    public Guid StatusId { get; init; }
}
