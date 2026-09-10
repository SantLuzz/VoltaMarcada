using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class CompanyLicenseConfiguration : IEntityTypeConfiguration<CompanyLicense>
{
    public void Configure(EntityTypeBuilder<CompanyLicense> builder)
    {
        builder.ToTable("CompanyLicenses");
        builder.HasKey(license => license.Id);
        builder.Property(license => license.Id).ValueGeneratedNever();
        builder.Property(license => license.CompanyId).IsRequired();
        builder.Property(license => license.Status).IsRequired();
        builder.Property(license => license.TrialStartedAtUtc).IsRequired();
        builder.Property(license => license.TrialEndsAtUtc).IsRequired();
        builder.Property(license => license.PaidUntilUtc).IsRequired(false);
        builder.Property(license => license.CreatedAtUtc).IsRequired();
        builder.Property(license => license.UpdatedAtUtc).IsRequired();

        builder.HasIndex(license => license.CompanyId)
            .HasDatabaseName("UX_CompanyLicenses_CompanyId")
            .IsUnique();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(license => license.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
