using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace RepairCenter.Data;

public sealed class RepairCenterDesignTimeDbContextFactory : IDesignTimeDbContextFactory<RepairCenterDbContext>
{
    public RepairCenterDbContext CreateDbContext(string[] args)
    {
        var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Development";
        var configuration = new ConfigurationBuilder()
            .SetBasePath(Directory.GetCurrentDirectory())
            .AddJsonFile("appsettings.json", optional: true)
            .AddJsonFile($"appsettings.{environment}.json", optional: true)
            .AddEnvironmentVariables()
            .Build();
        var connection = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");
        var options = new DbContextOptionsBuilder<RepairCenterDbContext>()
            .UseSqlServer(connection)
            .Options;
        return new RepairCenterDbContext(options);
    }
}
