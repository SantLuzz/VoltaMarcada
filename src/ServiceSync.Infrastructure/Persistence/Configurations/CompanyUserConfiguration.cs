using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;
using ServiceSync.Infrastructure.Identity;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class CompanyUserConfiguration : IEntityTypeConfiguration<CompanyUser>
{
    public void Configure(EntityTypeBuilder<CompanyUser> builder)
    {
        builder.ToTable("CompanyUsers");
        builder.HasKey(companyUser => companyUser.Id);
        builder.Property(companyUser => companyUser.Id).ValueGeneratedNever();
        builder.Property(companyUser => companyUser.Role).IsRequired();
        builder.Property(companyUser => companyUser.CreatedAtUtc).IsRequired();

        builder.HasOne<ApplicationUser>()
            .WithMany()
            .HasForeignKey(companyUser => companyUser.UserId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(companyUser => companyUser.CompanyId)
            .OnDelete(DeleteBehavior.Restrict)
            .IsRequired();

        builder.HasIndex(companyUser => new { companyUser.UserId, companyUser.CompanyId })
            .HasDatabaseName("UX_CompanyUsers_UserId_CompanyId")
            .IsUnique();
    }
}
