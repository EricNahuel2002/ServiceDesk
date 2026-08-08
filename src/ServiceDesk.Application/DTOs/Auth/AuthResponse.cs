namespace ServiceDesk.Application.DTOs.Auth;

public sealed record AuthResponse
{
    public string AccessToken { get; init; } = string.Empty;

    public string RefreshToken { get; init; } = string.Empty;

    public DateTime AccessTokenExpiresAtUtc { get; init; }

    public DateTime RefreshTokenExpiresAtUtc { get; init; }

    public AuthUserDto User { get; init; } = new();
}
