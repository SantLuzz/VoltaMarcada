using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class LicenseOverdueNotificationConfiguration : IEntityTypeConfiguration<LicenseOverdueNotification>
{
    public void Configure(EntityTypeBuilder<LicenseOverdueNotification> builder)
    {
        builder.ToTable("LicenseOverdueNotifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Id).ValueGeneratedNever();
        builder.HasIndex(notification => new { notification.CompanyLicenseId, notification.OwnerUserId, notification.SentOn }).IsUnique();
    }
}
