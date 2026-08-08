namespace ServiceDesk.Application.DTOs.Auth;

public sealed record AuthUserDto
{
    public Guid Id { get; init; }

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public string Email { get; init; } = string.Empty;

    public Guid CompanyId { get; init; }

    public string Role { get; init; } = string.Empty;
}
