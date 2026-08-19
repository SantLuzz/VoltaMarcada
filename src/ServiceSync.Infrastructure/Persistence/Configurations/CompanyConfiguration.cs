using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(company => company.Id);
        builder.Property(company => company.Id).ValueGeneratedNever();
        builder.Property(company => company.LegalName).HasMaxLength(200).IsRequired();
        builder.Property(company => company.TradeName).HasMaxLength(200).IsRequired();
        builder.Property(company => company.Document).HasMaxLength(14).IsRequired();
        builder.Property(company => company.WhatsApp).HasMaxLength(20).IsRequired();
        builder.Property(company => company.PostalCode).HasMaxLength(8).IsRequired();
        builder.Property(company => company.Address).HasMaxLength(300).IsRequired();
        builder.Property(company => company.LogoKey).HasMaxLength(500).IsRequired(false);
        builder.Property(company => company.Slug).HasMaxLength(200).IsRequired();
        builder.Property(company => company.CreatedAtUtc).IsRequired();
        builder.Property(company => company.UpdatedAtUtc).IsRequired();

        builder.HasIndex(company => company.Document)
            .HasDatabaseName("UX_Companies_Document")
            .IsUnique();

        builder.HasIndex(company => company.Slug)
            .HasDatabaseName("UX_Companies_Slug")
            .IsUnique();

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(company => company.StateId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<City>()
            .WithMany()
            .HasForeignKey(company => company.CityId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
