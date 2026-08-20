using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class EquipmentConfiguration : IEntityTypeConfiguration<Equipment>
{
    public void Configure(EntityTypeBuilder<Equipment> builder)
    {
        builder.ToTable("Equipment");
        builder.HasKey(equipment => equipment.Id);
        builder.Property(equipment => equipment.Id).ValueGeneratedNever();
        builder.Property(equipment => equipment.CompanyId).IsRequired();
        builder.Property(equipment => equipment.CustomerId).IsRequired();
        builder.Property(equipment => equipment.Type).HasMaxLength(200).IsRequired();
        builder.Property(equipment => equipment.Manufacturer).HasMaxLength(200).IsRequired(false);
        builder.Property(equipment => equipment.Model).HasMaxLength(200).IsRequired(false);
        builder.Property(equipment => equipment.SerialNumber).HasMaxLength(200).IsRequired(false);
        builder.Property(equipment => equipment.Capacity).HasMaxLength(100).IsRequired(false);
        builder.Property(equipment => equipment.LocationDescription).HasMaxLength(500).IsRequired(false);
        builder.Property(equipment => equipment.InstallationDate).HasColumnType("date").IsRequired(false);
        builder.Property(equipment => equipment.Notes).HasMaxLength(2_000).IsRequired(false);
        builder.Property(equipment => equipment.CreatedAtUtc).IsRequired();
        builder.Property(equipment => equipment.UpdatedAtUtc).IsRequired();

        builder.HasIndex(equipment => new { equipment.CompanyId, equipment.CustomerId })
            .HasDatabaseName("IX_Equipment_CompanyId_CustomerId");

        builder.HasIndex(equipment => new { equipment.CompanyId, equipment.Type })
            .HasDatabaseName("IX_Equipment_CompanyId_Type");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(equipment => equipment.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Customer>()
            .WithMany()
            .HasForeignKey(equipment => new { equipment.CompanyId, equipment.CustomerId })
            .HasPrincipalKey(customer => new { customer.CompanyId, customer.Id })
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
