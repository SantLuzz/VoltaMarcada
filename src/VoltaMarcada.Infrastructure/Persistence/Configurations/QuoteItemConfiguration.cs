using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class QuoteItemConfiguration : IEntityTypeConfiguration<QuoteItem>
{
    public void Configure(EntityTypeBuilder<QuoteItem> builder)
    {
        builder.ToTable("QuoteItems"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Description).HasMaxLength(500).IsRequired(); builder.Property(x => x.Quantity).HasPrecision(18, 2); builder.Property(x => x.UnitPrice).HasPrecision(18, 2); builder.Property(x => x.Type).IsRequired();
        builder.Property(x => x.EquipmentId).IsRequired(false); builder.Property(x => x.ServiceId).IsRequired(false);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(x => new { x.CompanyId, x.EquipmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.Ignore(x => x.TotalAmount); builder.HasIndex(x => new { x.CompanyId, x.QuoteId, x.DisplayOrder });
        builder.HasOne<Service>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ServiceId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
