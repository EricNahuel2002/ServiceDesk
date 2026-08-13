using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IIdentityService
{
    Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default);

    Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> CheckPasswordAsync(ApplicationUser user, string password);

    Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default);

    Task<bool> RoleExistsAsync(string role, CancellationToken cancellationToken = default);

    Task<bool> IsInRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default);

    Task<IdentityOperationResult> CreateAsync(ApplicationUser user, string password);

    Task<IdentityOperationResult> AddToRoleAsync(ApplicationUser user, string role);
}
