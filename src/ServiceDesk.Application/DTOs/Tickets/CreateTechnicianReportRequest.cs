namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record CreateTechnicianReportRequest
{
    public string? Reason { get; init; }

    public IReadOnlyList<TicketFileUpload> Files { get; init; } = [];
}
