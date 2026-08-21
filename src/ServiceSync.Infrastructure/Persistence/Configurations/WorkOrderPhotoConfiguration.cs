using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderPhotoConfiguration : IEntityTypeConfiguration<WorkOrderPhoto>
{
    public void Configure(EntityTypeBuilder<WorkOrderPhoto> builder)
    {
        builder.ToTable("WorkOrderPhotos"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.FileKey).HasMaxLength(1000).IsRequired(); builder.Property(x => x.Description).HasMaxLength(1000);
        builder.HasIndex(x => new { x.CompanyId, x.WorkOrderId, x.Type });
    }
}
