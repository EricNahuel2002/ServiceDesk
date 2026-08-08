namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record TicketAttachmentDto
{
    public Guid Id { get; init; }

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;

    public long SizeInBytes { get; init; }

    public string BlobUrl { get; init; } = string.Empty;
}
