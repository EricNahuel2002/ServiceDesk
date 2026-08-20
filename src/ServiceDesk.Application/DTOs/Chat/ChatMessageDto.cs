namespace ServiceDesk.Application.DTOs.Chat;

public sealed record ChatMessageDto
{
    public Guid Id { get; init; }

    public Guid TicketId { get; init; }

    public Guid SenderId { get; init; }

    public string SenderFirstName { get; init; } = string.Empty;

    public string SenderLastName { get; init; } = string.Empty;

    public string Content { get; init; } = string.Empty;

    public DateTime SentAtUtc { get; init; }
}
