using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class CustomerContactConfiguration : IEntityTypeConfiguration<CustomerContact>
{
    public void Configure(EntityTypeBuilder<CustomerContact> builder)
    {
        builder.ToTable("CustomerContacts");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CompanyId).IsRequired();
        builder.Property(x => x.CustomerId).IsRequired();
        builder.Property(x => x.OpportunityId).IsRequired();
        builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
        builder.HasIndex(x => new { x.CompanyId, x.OpportunityId, x.CreatedAtUtc })
            .HasDatabaseName("IX_CustomerContacts_CompanyId_OpportunityId_CreatedAtUtc");
        builder.HasIndex(x => new { x.CompanyId, x.CustomerId, x.CreatedAtUtc })
            .HasDatabaseName("IX_CustomerContacts_CompanyId_CustomerId_CreatedAtUtc");
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => new { x.CompanyId, x.CustomerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<MaintenanceOpportunity>().WithMany().HasForeignKey(x => new { x.CompanyId, x.OpportunityId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
