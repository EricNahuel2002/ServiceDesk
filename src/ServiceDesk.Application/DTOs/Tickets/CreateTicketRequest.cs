namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record CreateTicketRequest
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid CategoryId { get; init; }

    public IReadOnlyList<TicketFileUpload> Files { get; init; } = [];
}
