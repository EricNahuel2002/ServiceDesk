namespace ServiceDesk.Application.DTOs.Tickets;

public sealed record TicketDto
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public Guid CompanyId { get; init; }

    public Guid CategoryId { get; init; }

    public string CategoryName { get; init; } = string.Empty;

    public Guid PriorityId { get; init; }

    public string PriorityName { get; init; } = string.Empty;

    public Guid StatusId { get; init; }

    public string StatusName { get; init; } = string.Empty;

    public Guid CreatedById { get; init; }

    public Guid? AssignedToId { get; init; }

    public DateTime CreatedAtUtc { get; init; }

    public DateTime? UpdatedAtUtc { get; init; }

    public IReadOnlyList<TicketAttachmentDto> Attachments { get; init; } = [];
}
