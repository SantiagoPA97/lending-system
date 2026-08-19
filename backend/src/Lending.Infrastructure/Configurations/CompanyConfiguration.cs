using Lending.Domain;
using Lending.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NpgsqlTypes;

namespace Lending.Infrastructure.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Name).HasMaxLength(200).IsRequired();
        builder.Property(c => c.LegalName).HasMaxLength(300).IsRequired();
        builder.Property(c => c.RegistrationNumber).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Country).HasMaxLength(100).IsRequired();
        builder.Property(c => c.Industry).HasMaxLength(100).IsRequired();
        builder.Property(c => c.ContactEmail).HasMaxLength(320).IsRequired();
        builder.Property(c => c.Status).HasConversion<string>().HasMaxLength(20);
        builder.Property(c => c.Xmin).IsRowVersion();

        builder.HasIndex(c => c.Name).IsUnique();
        builder.HasIndex(c => c.Name, "ix_companies_name_trgm")
            .HasDatabaseName("ix_companies_name_trgm")
            .HasMethod("gin")
            .HasOperators("gin_trgm_ops");

        builder.Property<NpgsqlTsVector>("SearchVector")
            .IsGeneratedTsVectorColumn(
                "english",
                nameof(Company.Name),
                nameof(Company.LegalName),
                nameof(Company.RegistrationNumber),
                nameof(Company.Industry));
        builder.HasIndex("SearchVector").HasMethod("gin");
    }
}
