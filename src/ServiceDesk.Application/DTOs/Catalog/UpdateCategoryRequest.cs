namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record UpdateCategoryRequest
{
    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
