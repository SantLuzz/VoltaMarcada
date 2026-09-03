using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderAttachmentConfiguration : IEntityTypeConfiguration<WorkOrderAttachment>
{
    public void Configure(EntityTypeBuilder<WorkOrderAttachment> builder)
    {
        builder.ToTable("WorkOrderAttachments"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FileKey).HasMaxLength(1000).IsRequired(); builder.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired(); builder.Property(x => x.ContentType).HasMaxLength(255).IsRequired(); builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => new { x.CompanyId, x.WorkOrderId, x.CreatedAtUtc }).HasDatabaseName("IX_WorkOrderAttachments_CompanyId_WorkOrderId_CreatedAtUtc");
    }
}
