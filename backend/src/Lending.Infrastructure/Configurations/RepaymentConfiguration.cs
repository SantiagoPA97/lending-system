using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Lending.Infrastructure.Configurations;

public sealed class RepaymentConfiguration : IEntityTypeConfiguration<Repayment>
{
    public void Configure(EntityTypeBuilder<Repayment> builder)
    {
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Amount).HasColumnType("numeric(18,2)");
        builder.Property(r => r.PrincipalApplied).HasColumnType("numeric(18,2)");
        builder.Property(r => r.InterestApplied).HasColumnType("numeric(18,2)");
        builder.Property(r => r.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Property(r => r.Note).HasMaxLength(500);

        builder.HasIndex(r => r.ReversesRepaymentId);
        builder.HasIndex(r => r.PaymentDate);

        builder.OwnsMany(r => r.Allocations, a => a.ToJson("allocations"));
        builder.Navigation(r => r.Allocations).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
