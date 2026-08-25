using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class ProfessionalServiceConfiguration : IEntityTypeConfiguration<ProfessionalService>
{
    public void Configure(EntityTypeBuilder<ProfessionalService> builder)
    {
        builder.ToTable("ProfessionalServices");
        builder.HasKey(link => link.Id);
        builder.Property(link => link.Id).ValueGeneratedNever();
        builder.Property(link => link.CompanyId).IsRequired();
        builder.Property(link => link.ProfessionalId).IsRequired();
        builder.Property(link => link.ServiceId).IsRequired();

        builder.HasIndex(link => new { link.CompanyId, link.ProfessionalId, link.ServiceId })
            .IsUnique()
            .HasDatabaseName("UX_ProfessionalServices_CompanyId_ProfessionalId_ServiceId");

        builder.HasOne<Professional>()
            .WithMany(professional => professional.ProfessionalServices)
            .HasForeignKey(link => new { link.CompanyId, link.ProfessionalId })
            .HasPrincipalKey(professional => new { professional.CompanyId, professional.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasOne<Service>()
            .WithMany()
            .HasForeignKey(link => new { link.CompanyId, link.ServiceId })
            .HasPrincipalKey(service => new { service.CompanyId, service.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
