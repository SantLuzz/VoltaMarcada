using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class LicensePaymentConfiguration : IEntityTypeConfiguration<LicensePayment>
{
    public void Configure(EntityTypeBuilder<LicensePayment> builder)
    {
        builder.ToTable("LicensePayments");
        builder.HasKey(payment => payment.Id);
        builder.Property(payment => payment.Id).ValueGeneratedNever();
        builder.Property(payment => payment.CompanyId).IsRequired();
        builder.Property(payment => payment.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(payment => payment.Status).IsRequired();
        builder.Property(payment => payment.ProofFileKey).HasMaxLength(512).IsRequired();
        builder.Property(payment => payment.ProofContentType).HasMaxLength(100).IsRequired();
        builder.Property(payment => payment.ProofLength).IsRequired();
        builder.Property(payment => payment.SubmittedAtUtc).IsRequired();
        builder.Property(payment => payment.DecisionReason).HasMaxLength(2000).IsRequired(false);
        builder.Property(payment => payment.DecidedAtUtc).IsRequired(false);
        builder.Property(payment => payment.DecidedByUserId).IsRequired(false);
        builder.Property(payment => payment.UpdatedAtUtc).IsRequired();

        builder.HasIndex(payment => new { payment.CompanyId, payment.SubmittedAtUtc });
        builder.HasIndex(payment => payment.CompanyId)
            .HasDatabaseName("UX_LicensePayments_CompanyId_Pending")
            .IsUnique()
            .HasFilter("\"Status\" = 0");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(payment => payment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
