using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceSync.Infrastructure.Identity;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.HasOne(user => user.Company)
            .WithMany()
            .HasForeignKey(user => user.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
