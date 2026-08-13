using Microsoft.EntityFrameworkCore;
using ServiceDesk.Application.Common.Interfaces;

namespace ServiceDesk.Infrastructure.Persistence.Repositories;

public sealed class CompanyRepository : ICompanyRepository
{
    private readonly ServiceDeskDbContext _context;

    public CompanyRepository(ServiceDeskDbContext context)
    {
        _context = context;
    }

    public async Task<bool> ExistsAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await _context.Companies.AnyAsync(company => company.Id == companyId, cancellationToken);
}
