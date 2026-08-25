using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class ServiceConfiguration : IEntityTypeConfiguration<Service>
{
    public void Configure(EntityTypeBuilder<Service> builder)
    {
        builder.ToTable("Services");
        builder.HasKey(service => service.Id);
        builder.Property(service => service.Id).ValueGeneratedNever();
        builder.Property(service => service.CompanyId).IsRequired();
        builder.Property(service => service.Name).HasMaxLength(200).IsRequired();
        builder.Property(service => service.Description).HasMaxLength(2_000).IsRequired(false);
        builder.Property(service => service.DurationMinutes).IsRequired();
        builder.Property(service => service.PriceType).IsRequired();
        builder.Property(service => service.BasePrice).HasPrecision(18, 2).IsRequired(false);
        builder.Property(service => service.AllowOnlineBooking).IsRequired();
        builder.Property(service => service.RequireBeforePhotos).IsRequired();
        builder.Property(service => service.RequireAfterPhotos).IsRequired();
        builder.Property(service => service.IsActive).IsRequired();
        builder.Property(service => service.CreatedAtUtc).IsRequired();
        builder.Property(service => service.UpdatedAtUtc).IsRequired();

        builder.HasAlternateKey(service => new { service.CompanyId, service.Id })
            .HasName("AK_Services_CompanyId_Id");

        builder.HasIndex(service => new { service.CompanyId, service.Name })
            .HasDatabaseName("IX_Services_CompanyId_Name");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(service => service.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(service => service.ChecklistItems)
            .WithOne()
            .HasForeignKey(item => new { item.CompanyId, item.ServiceId })
            .HasPrincipalKey(service => new { service.CompanyId, service.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();
    }
}
