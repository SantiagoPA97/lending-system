using Lending.Domain;
using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Lending.Infrastructure.Configurations;

public sealed class FacilityConfiguration : IEntityTypeConfiguration<Facility>
{
    public void Configure(EntityTypeBuilder<Facility> builder)
    {
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Reference).HasMaxLength(20).IsRequired();
        builder.Property(f => f.CommitmentAmount).HasColumnType("numeric(18,2)");
        builder.Property(f => f.OutstandingPrincipal).HasColumnType("numeric(18,2)");
        builder.Property(f => f.AnnualInterestRate).HasColumnType("numeric(9,4)");
        builder.Property(f => f.Currency).HasConversion<string>().HasMaxLength(3);
        builder.Property(f => f.RepaymentType).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(f => f.Xmin).IsRowVersion();

        builder.Ignore(f => f.Commitment);
        builder.Ignore(f => f.Outstanding);

        builder.HasIndex(f => f.Reference).IsUnique();
        builder.HasIndex(f => f.Reference, "ix_facilities_reference_trgm")
            .HasDatabaseName("ix_facilities_reference_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");
        builder.HasIndex(f => f.Status);

        builder.Property<NpgsqlTsVector>("SearchVector")
            .IsGeneratedTsVectorColumn("simple", nameof(Facility.Reference));
        builder.HasIndex("SearchVector").HasMethod("gin");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(f => f.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(f => f.Schedule)
            .WithOne()
            .HasForeignKey(i => i.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(f => f.Schedule).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(f => f.Repayments)
            .WithOne()
            .HasForeignKey(r => r.FacilityId)
            .OnDelete(DeleteBehavior.Restrict);
        builder.Navigation(f => f.Repayments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
