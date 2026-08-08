using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServiceDesk.Infrastructure.Persistence;

public sealed class ServiceDeskDbContextFactory : IDesignTimeDbContextFactory<ServiceDeskDbContext>
{
    public ServiceDeskDbContext CreateDbContext(string[] args)
    {
        DbContextOptions<ServiceDeskDbContext> options = new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseSqlServer("Server=(localdb)\\MSSQLLocalDB;Database=ServiceDesk;Trusted_Connection=True")
            .Options;

        return new ServiceDeskDbContext(options);
    }
}
