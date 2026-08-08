using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServiceDesk.Domain.Identity;
using ServiceDesk.Infrastructure.Configuration;
using ServiceDesk.Infrastructure.Services;

namespace ServiceDesk.UnitTests;

public class JwtTokenGeneratorTests
{
    private const string SecretKey = "test-secret-key-test-secret-key-test-secret-key";

    private static JwtTokenGenerator CreateSut(out JwtSettings settings)
    {
        settings = new JwtSettings
        {
            Issuer = "ServiceDesk.Tests",
            Audience = "ServiceDesk.Clients",
            SecretKey = SecretKey,
            AccessTokenExpirationMinutes = 15,
            RefreshTokenExpirationDays = 7
        };

        return new JwtTokenGenerator(Options.Create(settings));
    }

    private static ApplicationUser CreateUser() =>
        new()
        {
            Id = Guid.NewGuid(),
            UserName = "user@test.com",
            Email = "user@test.com",
            FirstName = "John",
            LastName = "Doe",
            CompanyId = Guid.NewGuid()
        };

    [Fact]
    public void GenerateAccessToken_ReturnsNonEmptyToken()
    {
        JwtTokenGenerator sut = CreateSut(out _);

        string token = sut.GenerateAccessToken(CreateUser(), Roles.Cliente);

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public void GenerateAccessToken_ContainsExpectedClaims()
    {
        JwtTokenGenerator sut = CreateSut(out _);
        ApplicationUser user = CreateUser();

        string token = sut.GenerateAccessToken(user, Roles.Cliente);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        Assert.Equal(user.Id.ToString(), jwt.Subject);
        Assert.Equal(user.Email, jwt.Claims.Single(c => c.Type == "email").Value);
        Assert.Equal(user.CompanyId.ToString(), jwt.Claims.Single(c => c.Type == "companyId").Value);
        Assert.Equal(Roles.Cliente, jwt.Claims.Single(c => c.Type == ClaimTypes.Role).Value);
    }

    [Fact]
    public void GenerateAccessToken_TokenIsValidatedWithIssuerAudienceAndSignature()
    {
        JwtTokenGenerator sut = CreateSut(out JwtSettings settings);

        string token = sut.GenerateAccessToken(CreateUser(), Roles.Administrador);

        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.SecretKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        };

        ClaimsPrincipal principal = new JwtSecurityTokenHandler().ValidateToken(token, parameters, out _);

        Assert.True(principal.Identity!.IsAuthenticated);
        Assert.True(principal.IsInRole(Roles.Administrador));
    }

    [Fact]
    public void GenerateAccessToken_TokenExpiresWithinConfiguredWindow()
    {
        JwtTokenGenerator sut = CreateSut(out _);

        string token = sut.GenerateAccessToken(CreateUser(), Roles.Cliente);
        JwtSecurityToken jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);
        DateTime now = DateTime.UtcNow;

        Assert.True(jwt.ValidTo > now);
        Assert.True(jwt.ValidTo <= now.AddMinutes(16));
    }
}
