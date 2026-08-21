using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class ProfessionalConfiguration : IEntityTypeConfiguration<Professional>
{
    public void Configure(EntityTypeBuilder<Professional> builder)
    {
        builder.ToTable("Professionals");
        builder.HasKey(professional => professional.Id);
        builder.Property(professional => professional.Id).ValueGeneratedNever();
        builder.Property(professional => professional.CompanyId).IsRequired();
        builder.Property(professional => professional.CompanyUserId).IsRequired();
        builder.Property(professional => professional.IsActive).IsRequired();
        builder.Property(professional => professional.CreatedAtUtc).IsRequired();
        builder.Property(professional => professional.UpdatedAtUtc).IsRequired();

        builder.HasAlternateKey(professional => new { professional.CompanyId, professional.Id })
            .HasName("AK_Professionals_CompanyId_Id");

        builder.HasIndex(professional => new { professional.CompanyId, professional.CompanyUserId })
            .IsUnique()
            .HasDatabaseName("UX_Professionals_CompanyId_CompanyUserId");

        builder.HasIndex(professional => new { professional.CompanyId, professional.IsActive })
            .HasDatabaseName("IX_Professionals_CompanyId_IsActive");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(professional => professional.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<CompanyUser>()
            .WithMany()
            .HasForeignKey(professional => new { professional.CompanyId, professional.CompanyUserId })
            .HasPrincipalKey(companyUser => new { companyUser.CompanyId, companyUser.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(professional => professional.ProfessionalServices)
            .WithOne()
            .HasForeignKey(link => new { link.CompanyId, link.ProfessionalId })
            .HasPrincipalKey(professional => new { professional.CompanyId, professional.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Navigation(professional => professional.ProfessionalServices)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
