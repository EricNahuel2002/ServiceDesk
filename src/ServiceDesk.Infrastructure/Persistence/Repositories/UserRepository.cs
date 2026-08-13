using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
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
}
