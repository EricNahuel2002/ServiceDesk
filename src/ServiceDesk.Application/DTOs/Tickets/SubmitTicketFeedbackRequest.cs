namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record SubmitTicketFeedbackRequest
{
    public bool WasSolved { get; init; }

    public int? Rating { get; init; }

    public string? Comment { get; init; }
}
