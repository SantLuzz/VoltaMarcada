using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> builder)
    {
        builder.ToTable("Customers");
        builder.HasKey(customer => customer.Id);
        builder.Property(customer => customer.Id).ValueGeneratedNever();
        builder.Property(customer => customer.CompanyId).IsRequired();
        builder.Property(customer => customer.Name).HasMaxLength(200).IsRequired();
        builder.Property(customer => customer.Document).HasMaxLength(14).IsRequired();
        builder.Property(customer => customer.WhatsApp).HasMaxLength(20).IsRequired();
        builder.Property(customer => customer.Email).HasMaxLength(320).IsRequired(false);
        builder.Property(customer => customer.Notes).HasMaxLength(2_000).IsRequired(false);
        builder.Property(customer => customer.CreatedAtUtc).IsRequired();
        builder.Property(customer => customer.UpdatedAtUtc).IsRequired();

        builder.HasIndex(customer => new { customer.CompanyId, customer.Name })
            .HasDatabaseName("IX_Customers_CompanyId_Name");

        builder.HasIndex(customer => new { customer.CompanyId, customer.Document })
            .IsUnique()
            .HasDatabaseName("UX_Customers_CompanyId_Document");

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(customer => customer.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasMany(customer => customer.Addresses)
            .WithOne()
            .HasForeignKey(address => address.CustomerId)
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Navigation(customer => customer.Addresses)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
