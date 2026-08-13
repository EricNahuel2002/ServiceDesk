namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record ResolveTicketRequest
{
    public string? ResolutionNote { get; init; }
}
