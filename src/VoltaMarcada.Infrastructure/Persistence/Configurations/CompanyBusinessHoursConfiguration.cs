using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class CompanyBusinessHoursConfiguration : IEntityTypeConfiguration<CompanyBusinessHours>
{
    public void Configure(EntityTypeBuilder<CompanyBusinessHours> builder)
    {
        builder.ToTable("CompanyBusinessHours");
        builder.HasKey(item => new { item.CompanyId, item.DayOfWeek });
        builder.Property(item => item.IsOpen).IsRequired();
        builder.Property(item => item.StartTime).HasColumnType("time without time zone");
        builder.Property(item => item.EndTime).HasColumnType("time without time zone");
        builder.HasOne<Company>().WithMany(company => company.BusinessHours).HasForeignKey(item => item.CompanyId).OnDelete(DeleteBehavior.Cascade);
    }
}
