namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record CreateStatusRequest
{
    public string Name { get; init; } = string.Empty;

    public int SortOrder { get; init; }

    public bool IsClosed { get; init; }
}
