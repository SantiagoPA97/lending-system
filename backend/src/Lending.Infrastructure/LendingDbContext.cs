using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Lending.Infrastructure;

public class LendingDbContext(DbContextOptions<LendingDbContext> options) : DbContext(options)
{
    public const string FacilityReferenceSequence = "facility_reference_seq";

    public DbSet<Company> Companies => Set<Company>();
    public DbSet<Facility> Facilities => Set<Facility>();
    public DbSet<RepaymentScheduleItem> ScheduleItems => Set<RepaymentScheduleItem>();
    public DbSet<Repayment> Repayments => Set<Repayment>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasPostgresExtension("pg_trgm");
        modelBuilder.HasPostgresExtension("unaccent");
        modelBuilder.HasSequence<long>(FacilityReferenceSequence).StartsAt(1);
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(LendingDbContext).Assembly);
    }

    public override int SaveChanges(bool acceptAllChangesOnSuccess)
    {
        foreach (var facility in PendingFacilities())
        {
            var next = NextReferenceQuery().Single();
            facility.Reference = FormatReference(next);
        }
        TouchTimestamps();
        return base.SaveChanges(acceptAllChangesOnSuccess);
    }

    public override async Task<int> SaveChangesAsync(
        bool acceptAllChangesOnSuccess,
        CancellationToken cancellationToken = default)
    {
        foreach (var facility in PendingFacilities())
        {
            var next = await NextReferenceQuery().SingleAsync(cancellationToken);
            facility.Reference = FormatReference(next);
        }
        TouchTimestamps();
        return await base.SaveChangesAsync(acceptAllChangesOnSuccess, cancellationToken);
    }

    private List<Facility> PendingFacilities() =>
        ChangeTracker.Entries<Facility>()
            .Where(e => e.State == EntityState.Added && string.IsNullOrEmpty(e.Entity.Reference))
            .Select(e => e.Entity)
            .ToList();

    private IQueryable<long> NextReferenceQuery() =>
        Database.SqlQueryRaw<long>($"""SELECT nextval('{FacilityReferenceSequence}') AS "Value" """);

    private static string FormatReference(long value) => $"FAC-{value:00000}";

    private void TouchTimestamps()
    {
        var now = DateTime.UtcNow;
        foreach (var entry in ChangeTracker.Entries())
        {
            if (entry.State != EntityState.Modified)
                continue;
            if (entry.Metadata.FindProperty("UpdatedAtUtc") is not null)
                entry.Property("UpdatedAtUtc").CurrentValue = now;
        }
    }
}
