using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class MaintenanceOpportunityConfiguration : IEntityTypeConfiguration<MaintenanceOpportunity>
{
    public void Configure(EntityTypeBuilder<MaintenanceOpportunity> builder)
    {
        builder.ToTable("MaintenanceOpportunities");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.EquipmentId).IsRequired();
        builder.Property(x => x.PreviousWorkOrderId).IsRequired();
        builder.Property(x => x.RecommendedServiceId).IsRequired();
        builder.Property(x => x.DueDate).IsRequired();
        builder.Property(x => x.Notes).HasMaxLength(2000).IsRequired();
        builder.HasAlternateKey(x => new { x.CompanyId, x.Id })
            .HasName("AK_MaintenanceOpportunities_CompanyId_Id");
        builder.HasIndex(x => new { x.CompanyId, x.Status, x.DueDate })
            .HasDatabaseName("IX_MaintenanceOpportunities_CompanyId_Status_DueDate");
        builder.HasIndex(x => new { x.CompanyId, x.CustomerId })
            .HasDatabaseName("IX_MaintenanceOpportunities_CompanyId_CustomerId");
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => new { x.CompanyId, x.CustomerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(x => new { x.CompanyId, x.EquipmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkOrder>().WithMany().HasForeignKey(x => new { x.CompanyId, x.PreviousWorkOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Service>().WithMany().HasForeignKey(x => new { x.CompanyId, x.RecommendedServiceId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
