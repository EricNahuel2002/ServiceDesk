using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.DTOs.Users;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IUserRepository
{
    Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<UserListItemDto>> GetAllByCompanyIdAsync(Guid companyId, CancellationToken cancellationToken = default);
}
