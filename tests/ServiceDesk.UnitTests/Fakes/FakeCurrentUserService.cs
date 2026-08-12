using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.UnitTests.Fakes;

internal sealed class FakeCurrentUserService(Guid userId, Guid companyId) : ICurrentUserService
{
    public bool IsAuthenticated => true;

    public Guid UserId { get; } = userId;

    public Guid CompanyId { get; } = companyId;
}
