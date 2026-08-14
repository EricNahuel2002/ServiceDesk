namespace ServiceDesk.Application.DTOs.Tickets;

public sealed class AttachmentDownloadResult
{
    public required Stream Content { get; init; }

    public required string ContentType { get; init; }

    public required string FileName { get; init; }
}
