using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class ServiceChecklistItemConfiguration : IEntityTypeConfiguration<ServiceChecklistItem>
{
    public void Configure(EntityTypeBuilder<ServiceChecklistItem> builder)
    {
        builder.ToTable("ServiceChecklistItems");
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.CompanyId).IsRequired();
        builder.Property(item => item.ServiceId).IsRequired();
        builder.Property(item => item.Description).HasMaxLength(500).IsRequired();
        builder.Property(item => item.IsRequired).IsRequired();
        builder.Property(item => item.DisplayOrder).IsRequired();

        builder.HasIndex(item => new { item.CompanyId, item.ServiceId, item.DisplayOrder })
            .HasDatabaseName("IX_ServiceChecklistItems_CompanyId_ServiceId_DisplayOrder");
    }
}
