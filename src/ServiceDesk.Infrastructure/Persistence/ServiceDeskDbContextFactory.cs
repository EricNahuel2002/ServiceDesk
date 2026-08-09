using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ServiceDesk.Infrastructure.Persistence;

public sealed class ServiceDeskDbContextFactory : IDesignTimeDbContextFactory<ServiceDeskDbContext>
{
    public ServiceDeskDbContext CreateDbContext(string[] args)
    {
        IConfigurationRoot configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: false)
            .Build();

        DbContextOptions<ServiceDeskDbContext> options = new DbContextOptionsBuilder<ServiceDeskDbContext>()
            .UseSqlServer(configuration.GetConnectionString("DefaultConnection"))
            .Options;

        return new ServiceDeskDbContext(options);
    }
}
