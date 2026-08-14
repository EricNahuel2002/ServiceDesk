namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record TicketFileUpload
{
    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeInBytes { get; init; }

    public Stream Content { get; init; } = Stream.Null;
}
