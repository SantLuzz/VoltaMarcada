using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class ProfessionalDailyScheduleConfiguration : IEntityTypeConfiguration<ProfessionalDailySchedule>
{
    public void Configure(EntityTypeBuilder<ProfessionalDailySchedule> builder)
    {
        builder.ToTable("ProfessionalDailySchedules");
        builder.HasKey(schedule => schedule.Id);
        builder.Property(schedule => schedule.Id).ValueGeneratedNever();
        builder.Property(schedule => schedule.CompanyId).IsRequired();
        builder.Property(schedule => schedule.ProfessionalAvailabilityId).IsRequired();
        builder.Property(schedule => schedule.Weekday).IsRequired();
        builder.Property(schedule => schedule.EntryTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(schedule => schedule.ExitTime).HasColumnType("time without time zone").IsRequired();
        builder.Property(schedule => schedule.LunchStartTime).HasColumnType("time without time zone");
        builder.Property(schedule => schedule.LunchEndTime).HasColumnType("time without time zone");

        builder.HasIndex(schedule => new { schedule.CompanyId, schedule.ProfessionalAvailabilityId, schedule.Weekday })
            .IsUnique()
            .HasDatabaseName("UX_ProfessionalDailySchedules_CompanyId_AvailabilityId_Weekday");
    }
}
