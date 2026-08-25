using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class CustomerAddressConfiguration : IEntityTypeConfiguration<CustomerAddress>
{
    public void Configure(EntityTypeBuilder<CustomerAddress> builder)
    {
        builder.ToTable("CustomerAddresses");
        builder.HasKey(address => address.Id);
        builder.Property(address => address.Id).ValueGeneratedNever();
        builder.Property(address => address.CustomerId).IsRequired();
        builder.Property(address => address.AddressType).IsRequired();
        builder.Property(address => address.IsPrimary).IsRequired();
        builder.Property(address => address.Street).HasMaxLength(200).IsRequired();
        builder.Property(address => address.Number).HasMaxLength(50).IsRequired();
        builder.Property(address => address.PostalCode).HasMaxLength(8).IsRequired();
        builder.Property(address => address.Complement).HasMaxLength(200).IsRequired(false);
        builder.Property(address => address.StateId).IsRequired();
        builder.Property(address => address.CityId).IsRequired();

        builder.HasIndex(address => address.CustomerId)
            .HasDatabaseName("IX_CustomerAddresses_CustomerId");

        builder.HasIndex(address => new { address.CustomerId, address.IsPrimary })
            .HasDatabaseName("UX_CustomerAddresses_CustomerId_Primary")
            .IsUnique()
            .HasFilter("\"IsPrimary\" = true");

        builder.HasOne(address => address.State)
            .WithMany()
            .HasForeignKey(address => address.StateId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne(address => address.City)
            .WithMany()
            .HasForeignKey(address => address.CityId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();
    }
}
