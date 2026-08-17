namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record UpdatePriorityRequest
{
    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsActive { get; init; }
}
