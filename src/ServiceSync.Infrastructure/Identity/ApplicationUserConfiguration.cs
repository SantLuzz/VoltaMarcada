using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ServiceSync.Infrastructure.Identity;

public sealed class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("AspNetUsers");
        builder.Property(user => user.FullName).HasMaxLength(200).IsRequired(false);
        builder.Property(user => user.Document).HasMaxLength(11).IsRequired(false);
        builder.Property(user => user.Phone).HasMaxLength(20).IsRequired(false);
        builder.Property(user => user.Address).HasMaxLength(300).IsRequired(false);
    }
}
