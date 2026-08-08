namespace ServiceDesk.Application.DTOs.Auth;

public sealed record RefreshTokenRequest
{
    public string RefreshToken { get; init; } = string.Empty;
}
