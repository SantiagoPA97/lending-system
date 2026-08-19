using Lending.Domain;
using Lending.Infrastructure.Search;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Lending.Infrastructure;

public static class InfrastructureServiceCollectionExtensions
{
    public static IServiceCollection AddLendingInfrastructure(
        this IServiceCollection services,
        string connectionString)
    {
        services.TryAddScoped<ICurrentUser, SystemCurrentUser>();
        services.TryAddSingleton<IScheduleCalculator, ScheduleCalculator>();
        services.TryAddScoped<ISearchService, PostgresSearchService>();
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<LendingDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
        return services;
    }
}
