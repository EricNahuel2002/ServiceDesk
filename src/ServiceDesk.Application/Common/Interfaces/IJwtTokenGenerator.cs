using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IJwtTokenGenerator
{
    string GenerateAccessToken(ApplicationUser user, string role);
}
