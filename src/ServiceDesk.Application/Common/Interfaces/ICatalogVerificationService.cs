using ServiceDesk.Domain.Catalog;

namespace ServiceDesk.Application.Common.Interfaces;

public interface ICatalogVerificationService
{
    Task EnsureCategoryBelongsToCompanyAsync(Guid categoryId, CancellationToken cancellationToken = default);

    Task EnsurePriorityBelongsToCompanyAsync(Guid priorityId, CancellationToken cancellationToken = default);

    Task<Status> EnsureStatusBelongsToCompanyAsync(Guid statusId, CancellationToken cancellationToken = default);
}
