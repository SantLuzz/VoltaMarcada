using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class ProfessionalAvailabilityConfiguration : IEntityTypeConfiguration<ProfessionalAvailability>
{
    public void Configure(EntityTypeBuilder<ProfessionalAvailability> builder)
    {
        builder.ToTable("ProfessionalAvailabilities");
        builder.HasKey(availability => availability.Id);
        builder.Property(availability => availability.Id).ValueGeneratedNever();
        builder.Property(availability => availability.CompanyId).IsRequired();
        builder.Property(availability => availability.ProfessionalId).IsRequired();
        builder.Property(availability => availability.CreatedAtUtc).IsRequired();
        builder.Property(availability => availability.UpdatedAtUtc).IsRequired();

        builder.HasAlternateKey(availability => new { availability.CompanyId, availability.Id })
            .HasName("AK_ProfessionalAvailabilities_CompanyId_Id");

        builder.HasIndex(availability => new { availability.CompanyId, availability.ProfessionalId })
            .IsUnique()
            .HasDatabaseName("UX_ProfessionalAvailabilities_CompanyId_ProfessionalId");

        builder.HasOne<Professional>()
            .WithOne()
            .HasForeignKey<ProfessionalAvailability>(availability => new { availability.CompanyId, availability.ProfessionalId })
            .HasPrincipalKey<Professional>(professional => new { professional.CompanyId, professional.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.HasMany(availability => availability.DailySchedules)
            .WithOne()
            .HasForeignKey(schedule => new { schedule.CompanyId, schedule.ProfessionalAvailabilityId })
            .HasPrincipalKey(availability => new { availability.CompanyId, availability.Id })
            .OnDelete(DeleteBehavior.Cascade)
            .IsRequired();

        builder.Navigation(availability => availability.DailySchedules)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
