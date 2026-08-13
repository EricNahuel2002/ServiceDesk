using ServiceDesk.Domain.Identity;

namespace ServiceDesk.Application.Common.Interfaces;

public interface IRefreshTokenRepository
{
    Task<RefreshToken?> GetByTokenHashAsync(string tokenHash, CancellationToken cancellationToken = default);

    void Add(RefreshToken token);
}
