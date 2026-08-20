using ServiceDesk.Application.Common.Interfaces;
using ServiceDesk.Application.DTOs.Tickets;
using ServiceDesk.Application.DTOs.Users;
using ServiceDesk.Domain.Identity;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeUserRepository : IUserRepository
{
    public ApplicationUser? User { get; set; }

    public List<ApplicationUser> Administrators { get; } = [];

    public Task<ApplicationUser?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        Task.FromResult(User);

    public Task<IReadOnlyList<TechnicianDto>> GetTechniciansAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();

    public Task<IReadOnlyList<ApplicationUser>> GetActiveAdministratorsAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<ApplicationUser>>(Administrators);

    public Task<IReadOnlyList<UserListItemDto>> GetAllByCompanyIdAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
}