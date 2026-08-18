using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class CompanyConfiguration : IEntityTypeConfiguration<Company>
{
    public void Configure(EntityTypeBuilder<Company> builder)
    {
        builder.ToTable("Companies");
        builder.HasKey(company => company.Id);
        builder.Property(company => company.Id).ValueGeneratedNever();
        builder.Property(company => company.Name).HasMaxLength(200).IsRequired();
        builder.Property(company => company.CreatedAtUtc).IsRequired();
    }
}
