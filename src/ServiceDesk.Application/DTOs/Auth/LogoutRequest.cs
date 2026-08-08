namespace ServiceDesk.Application.DTOs.Auth;

public sealed record LogoutRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
