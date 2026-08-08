namespace ServiceDesk.Application.DTOs.Auth;

public sealed record AdminCreateUserRequest
{
    public string Email { get; init; } = string.Empty;

    public string Password { get; init; } = string.Empty;

    public string FirstName { get; init; } = string.Empty;

    public string LastName { get; init; } = string.Empty;

    public Guid CompanyId { get; init; }

    public string Role { get; init; } = string.Empty;
}
