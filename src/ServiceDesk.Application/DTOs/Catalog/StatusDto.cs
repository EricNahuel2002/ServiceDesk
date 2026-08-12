namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record StatusDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsClosed { get; init; }

    public bool IsActive { get; init; }
}
