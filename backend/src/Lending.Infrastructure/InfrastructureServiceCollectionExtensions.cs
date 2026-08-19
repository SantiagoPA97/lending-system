using Lending.Domain;
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
        services.AddScoped<AuditSaveChangesInterceptor>();
        services.AddDbContext<LendingDbContext>((sp, options) => options
            .UseNpgsql(connectionString)
            .UseSnakeCaseNamingConvention()
            .AddInterceptors(sp.GetRequiredService<AuditSaveChangesInterceptor>()));
        return services;
    }
}
