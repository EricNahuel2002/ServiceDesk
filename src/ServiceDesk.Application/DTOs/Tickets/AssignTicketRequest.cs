namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record AssignTicketRequest
{
    public Guid AssignedToId { get; init; }
}
