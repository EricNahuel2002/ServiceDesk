namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record PriorityDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }
}
