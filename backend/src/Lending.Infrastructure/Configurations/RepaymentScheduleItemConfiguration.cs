using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Configurations;

public sealed class RepaymentScheduleItemConfiguration : IEntityTypeConfiguration<RepaymentScheduleItem>
{
    public void Configure(EntityTypeBuilder<RepaymentScheduleItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.PrincipalDue).HasColumnType("numeric(18,2)");
        builder.Property(i => i.InterestDue).HasColumnType("numeric(18,2)");
        builder.Property(i => i.RemainingBalance).HasColumnType("numeric(18,2)");
        builder.Property(i => i.PrincipalPaid).HasColumnType("numeric(18,2)");
        builder.Property(i => i.InterestPaid).HasColumnType("numeric(18,2)");
        builder.Property(i => i.InterestWaived).HasColumnType("numeric(18,2)");

        builder.HasIndex(i => new { i.FacilityId, i.Period }).IsUnique();
        builder.HasIndex(i => i.DueDate);
    }
}
