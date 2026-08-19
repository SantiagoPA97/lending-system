using System.Text.Json.Serialization;
using FluentValidation;
using Lending.Api.Common;
using Lending.Api.Features.Audit;
using Lending.Api.Features.Companies;
using Lending.Api.Features.Dashboard;
using Lending.Api.Features.Facilities;
using Lending.Api.Features.Repayments;
using Lending.Api.Features.Search;
using Lending.Infrastructure;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.EntityFrameworkCore;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

Log.Logger = new LoggerConfiguration()
    .WriteTo.Console(new CompactJsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((context, configuration) => configuration
        .MinimumLevel.Information()
        .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
        .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console(new CompactJsonFormatter()));

    var connectionString = ConnectionStringResolver.Resolve(builder.Configuration);

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUser, HttpCurrentUser>();
    builder.Services.AddLendingInfrastructure(connectionString);

    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddHybridCache();
    builder.Services.ConfigureHttpJsonOptions(options =>
        options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen();

    builder.Services.AddHealthChecks()
        .AddNpgSql(connectionString, name: "postgres", tags: ["ready"]);

    var otlpEndpoint = Environment.GetEnvironmentVariable("OTEL_EXPORTER_OTLP_ENDPOINT");
    if (!string.IsNullOrWhiteSpace(otlpEndpoint))
    {
        builder.Services.AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService("lending-api"))
            .WithTracing(tracing => tracing
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter())
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddOtlpExporter());
    }

    var app = builder.Build();

    app.UseMiddleware<CorrelationIdMiddleware>();
    app.UseSerilogRequestLogging();
    app.UseMiddleware<ExceptionHandlingMiddleware>();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    var wwwroot = Path.Combine(app.Environment.ContentRootPath, "wwwroot");
    var serveSpa = Directory.Exists(wwwroot);
    if (serveSpa)
    {
        app.UseDefaultFiles();
        app.UseStaticFiles();
    }

    var api = app.MapGroup("/api");
    api.MapGroup("/companies").MapCompanyEndpoints().WithTags("Companies");
    api.MapGroup("/facilities").MapFacilityEndpoints().WithTags("Facilities");
    api.MapGroup("/repayments").MapRepaymentEndpoints().WithTags("Repayments");
    api.MapGroup("/search").MapSearchEndpoints().WithTags("Search");
    api.MapGroup("/dashboard").MapDashboardEndpoints().WithTags("Dashboard");
    api.MapGroup("/audit").MapAuditEndpoints().WithTags("Audit");

    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = r => r.Tags.Contains("ready") });

    if (serveSpa)
        app.MapFallbackToFile("index.html");

    if (IsFlagSet("MIGRATE_ON_STARTUP") || IsFlagSet("SEED_DEMO_DATA"))
    {
        await using var scope = app.Services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<LendingDbContext>();
        if (IsFlagSet("MIGRATE_ON_STARTUP"))
        {
            Log.Information("Applying database migrations");
            await db.Database.MigrateAsync();
        }

        if (IsFlagSet("SEED_DEMO_DATA"))
        {
            Log.Information("Seeding demo data");
            await DemoDataSeeder.SeedAsync(db);
        }
    }

    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Host terminated unexpectedly");
    throw;
}
finally
{
    await Log.CloseAndFlushAsync();
}

static bool IsFlagSet(string variable)
{
    var value = Environment.GetEnvironmentVariable(variable);
    return value == "1" || string.Equals(value, "true", StringComparison.OrdinalIgnoreCase);
}

public partial class Program;
