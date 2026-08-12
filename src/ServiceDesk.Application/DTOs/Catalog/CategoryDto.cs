namespace ServiceDesk.Application.DTOs.Catalog;

public sealed record CategoryDto
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
