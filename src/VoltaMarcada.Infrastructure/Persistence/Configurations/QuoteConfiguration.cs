using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class QuoteConfiguration : IEntityTypeConfiguration<Quote>
{
    public void Configure(EntityTypeBuilder<Quote> builder)
    {
        builder.ToTable("Quotes"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CreatedByUserId).IsRequired(); builder.Property(x => x.Notes).HasMaxLength(4000); builder.Property(x => x.PublicTokenHash).HasMaxLength(128); builder.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        builder.HasAlternateKey(x => new { x.CompanyId, x.Id });
        builder.HasIndex(x => new { x.CompanyId, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.ServiceRequestId }).IsUnique().HasFilter("\"ServiceRequestId\" IS NOT NULL");
        builder.HasIndex(x => x.PublicTokenHash).IsUnique().HasFilter("\"PublicTokenHash\" IS NOT NULL");
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => new { x.CompanyId, x.CustomerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(x => new { x.CompanyId, x.EquipmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Service>().WithMany().HasForeignKey(x => new { x.CompanyId, x.PrimaryServiceId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<ServiceRequest>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ServiceRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkOrder>().WithMany().HasForeignKey(x => new { x.CompanyId, x.GeneratedWorkOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => new { x.CompanyId, x.QuoteId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
