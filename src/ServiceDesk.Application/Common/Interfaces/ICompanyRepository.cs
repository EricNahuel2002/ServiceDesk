namespace ServiceDesk.Application.Common.Interfaces;

public interface ICompanyRepository
{
    Task<bool> ExistsAsync(Guid companyId, CancellationToken cancellationToken = default);
}
