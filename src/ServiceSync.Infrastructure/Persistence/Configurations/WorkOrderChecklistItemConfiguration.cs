using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderChecklistItemConfiguration : IEntityTypeConfiguration<WorkOrderChecklistItem>
{
    public void Configure(EntityTypeBuilder<WorkOrderChecklistItem> builder)
    {
        builder.ToTable("WorkOrderChecklistItems"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired(); builder.HasIndex(x => new { x.CompanyId, x.WorkOrderId, x.DisplayOrder });
    }
}
