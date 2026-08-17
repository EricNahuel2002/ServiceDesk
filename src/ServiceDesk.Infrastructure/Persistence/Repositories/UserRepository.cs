using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.DTOs.Users;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly ServiceDeskDbContext _context;

    public UserRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        await _context.Users
            .AsNoTracking()
            .Include(user => user.Roles)
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);

    public async Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        List<TechnicianDto> technicians = await _context.Users
            .AsNoTracking()
            .Where(user => user.IsActive
                && user.CompanyId == companyId
                && _context.UserRoles.Any(userRole => userRole.UserId == user.Id && userRole.RoleId == _context.Roles
                    .Where(role => role.Name == Roles.Tecnico)
                    .Select(role => role.Id)
                    .FirstOrDefault()))
            .OrderBy(user => user.FirstName)
            .ThenBy(user => user.LastName)
            .Select(user => new TechnicianDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        return technicians;
    }

    public async Task<IReadOnlyList<UserListItemDto>> GetAllByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Where(user => user.CompanyId == companyId)
            .Join(
                _context.UserRoles,
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(
                _context.Roles,
                ur => ur.userRole.RoleId,
                role => role.Id,
                (ur, role) => new { ur.user, role })
            .Where(x => x.role.Name != Roles.Cliente)
            .OrderBy(x => x.user.FirstName)
            .ThenBy(x => x.user.LastName)
            .Select(x => new UserListItemDto
            {
                Id = x.user.Id,
                FirstName = x.user.FirstName,
                LastName = x.user.LastName,
                Email = x.user.Email ?? string.Empty,
                Role = x.role.Name ?? string.Empty,
                IsActive = x.user.IsActive,
                CreatedAtUtc = x.user.CreatedAtUtc
            })
            .ToListAsync(cancellationToken);
    }
}
