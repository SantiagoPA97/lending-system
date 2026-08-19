using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Lending.Infrastructure;

public sealed class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LendingDbContext>
{
    public LendingDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__lending")
            ?? "Host=localhost;Database=lending;Username=lending;Password=lending";

        var options = new DbContextOptionsBuilder<LendingDbContext>()
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .Options;

        return new LendingDbContext(options);
    }
}
