namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogVerificationService
{
    Task EnsureCategoryBelongsToCompanyAsync(Guid categoryId, CancellationToken cancellationToken = default);
}
