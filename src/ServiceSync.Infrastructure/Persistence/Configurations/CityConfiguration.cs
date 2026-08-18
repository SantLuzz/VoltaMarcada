using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class CityConfiguration : IEntityTypeConfiguration<City>
{
    public void Configure(EntityTypeBuilder<City> builder)
    {
        builder.ToTable("Cities");
        builder.HasKey(city => city.Id);
        builder.Property(city => city.Id).ValueGeneratedNever();
        builder.Property(city => city.Name).HasMaxLength(150).IsRequired();

        builder.HasOne<State>()
            .WithMany()
            .HasForeignKey(city => city.StateId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(city => new { city.StateId, city.Name })
            .HasDatabaseName("UX_Cities_StateId_Name")
            .IsUnique();
    }
}
