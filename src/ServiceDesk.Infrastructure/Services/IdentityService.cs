using Microsoft.AspNetCore.Identity;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Infrastructure.Services;

public sealed class IdentityService : IIdentityService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<ApplicationRole> _roleManager;

    public IdentityService(
        UserManager<ApplicationUser> userManager,
        RoleManager<ApplicationRole> roleManager)
    {
        _userManager = userManager;
        _roleManager = roleManager;
    }

    public Task<ApplicationUser?> FindByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        _userManager.FindByEmailAsync(email);

    public Task<ApplicationUser?> FindByIdAsync(Guid userId, CancellationToken cancellationToken = default) =>
        _userManager.FindByIdAsync(userId.ToString());

    public Task<bool> CheckPasswordAsync(ApplicationUser user, string password) =>
        _userManager.CheckPasswordAsync(user, password);

    public async Task<IReadOnlyList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken = default) =>
        (await _userManager.GetRolesAsync(user)).ToArray();

    public Task<bool> RoleExistsAsync(string role, CancellationToken cancellationToken = default) =>
        _roleManager.RoleExistsAsync(role);

    public Task<bool> IsInRoleAsync(ApplicationUser user, string role, CancellationToken cancellationToken = default) =>
        _userManager.IsInRoleAsync(user, role);

    public async Task<IdentityOperationResult> CreateAsync(ApplicationUser user, string password)
    {
        IdentityResult result = await _userManager.CreateAsync(user, password);
        return ToOperationResult(result);
    }

    public async Task<IdentityOperationResult> AddToRoleAsync(ApplicationUser user, string role)
    {
        IdentityResult result = await _userManager.AddToRoleAsync(user, role);
        return ToOperationResult(result);
    }

    private static IdentityOperationResult ToOperationResult(IdentityResult result)
    {
        if (result.Succeeded)
        {
            return IdentityOperationResult.Success();
        }

        IReadOnlyDictionary<string, string[]> errors = result.Errors
            .GroupBy(e => e.Code)
            .ToDictionary(group => group.Key, group => group.Select(e => e.Description).ToArray());

        return new IdentityOperationResult(false, errors);
    }
}
