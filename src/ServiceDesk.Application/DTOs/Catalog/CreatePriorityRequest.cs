namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record CreatePriorityRequest
{
    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }
}
