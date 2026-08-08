using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Infrastructure.Services;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public bool IsAuthenticated =>
        _httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;

    public Guid UserId => GetClaimGuid(JwtRegisteredClaimNames.Sub);

    public Guid CompanyId => GetClaimGuid(JwtClaimNames.CompanyId);

    private Guid GetClaimGuid(string claimType) =>
        Guid.TryParse(_httpContextAccessor.HttpContext?.User.FindFirstValue(claimType), out Guid value)
            ? value
            : Guid.Empty;
}
