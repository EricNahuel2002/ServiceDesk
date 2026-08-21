using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeIdentityService : IIdentityService
{
    public List<string> Roles { get; } = [];

    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult<ApplicationUser?>(null);

    public Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        Task.FromResult<ApplicationUser?>(null);

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) =>
        Task.FromResult(true);

    public Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<string>>(Roles);

    public Task<bool> RoleExistsAsync(string role, CancellationToken cancellationToken = default) =>
        Task.FromResult(Roles.Contains(role));

    public Task<bool> IsInRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default) =>
        Task.FromResult(Roles.Contains(role));

    public Task<IdentityOperationResult> CreateAsync(ApplicationUser user, string password) =>
        Task.FromResult(IdentityOperationResult.Success());

    public Task<IdentityOperationResult> AddToRoleAsync(ApplicationUser user, string role) =>
        Task.FromResult(IdentityOperationResult.Success());
}
