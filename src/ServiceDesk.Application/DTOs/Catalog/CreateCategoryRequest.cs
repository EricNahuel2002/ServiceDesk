namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record CreateCategoryRequest
{
    public string Name { get; init; } = string.Empty;
}
