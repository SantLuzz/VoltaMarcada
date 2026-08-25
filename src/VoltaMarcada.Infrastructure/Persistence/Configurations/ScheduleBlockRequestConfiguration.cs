using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class ScheduleBlockRequestConfiguration : IEntityTypeConfiguration<ScheduleBlockRequest>
{
    public void Configure(EntityTypeBuilder<ScheduleBlockRequest> builder)
    {
        builder.ToTable("ScheduleBlockRequests");
        builder.HasKey(request => request.Id);
        builder.Property(request => request.Id).ValueGeneratedNever();
        builder.Property(request => request.Notes).HasMaxLength(1000);
        builder.Property(request => request.RejectionReason).HasMaxLength(1000);
        builder.HasAlternateKey(request => new { request.CompanyId, request.Id }).HasName("AK_ScheduleBlockRequests_CompanyId_Id");
        builder.HasIndex(request => new { request.CompanyId, request.Status, request.CreatedAtUtc }).HasDatabaseName("IX_ScheduleBlockRequests_CompanyId_Status_CreatedAtUtc");
        builder.HasIndex(request => new { request.CompanyId, request.ProfessionalId, request.StartDate, request.EndDate }).HasDatabaseName("IX_ScheduleBlockRequests_CompanyId_ProfessionalId_StartDate_EndDate");
        builder.HasOne<Professional>().WithMany().HasForeignKey(request => new { request.CompanyId, request.ProfessionalId }).HasPrincipalKey(professional => new { professional.CompanyId, professional.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
