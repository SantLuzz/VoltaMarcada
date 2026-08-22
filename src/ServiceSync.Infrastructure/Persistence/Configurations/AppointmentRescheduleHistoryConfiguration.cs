using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class AppointmentRescheduleHistoryConfiguration : IEntityTypeConfiguration<AppointmentRescheduleHistory>
{
    public void Configure(EntityTypeBuilder<AppointmentRescheduleHistory> builder)
    {
        builder.ToTable("AppointmentRescheduleHistories");
        builder.HasKey(history => history.Id);
        builder.Property(history => history.Id).ValueGeneratedNever();
        builder.Property(history => history.Reason).HasMaxLength(1000).IsRequired();
        builder.HasIndex(history => new { history.CompanyId, history.AppointmentId, history.CreatedAtUtc }).HasDatabaseName("IX_AppointmentRescheduleHistories_CompanyId_AppointmentId_CreatedAtUtc");
    }
}
