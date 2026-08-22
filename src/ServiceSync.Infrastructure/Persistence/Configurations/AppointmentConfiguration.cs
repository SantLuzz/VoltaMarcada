using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Configurations;

public sealed class AppointmentConfiguration : IEntityTypeConfiguration<Appointment>
{
    public void Configure(EntityTypeBuilder<Appointment> builder)
    {
        builder.ToTable("Appointments");
        builder.HasKey(appointment => appointment.Id);
        builder.Property(appointment => appointment.Id).ValueGeneratedNever();
        builder.Property(appointment => appointment.Status).IsRequired();
        builder.Property(appointment => appointment.Notes).HasMaxLength(1000);
        builder.HasAlternateKey(appointment => new { appointment.CompanyId, appointment.Id }).HasName("AK_Appointments_CompanyId_Id");
        builder.HasIndex(appointment => new { appointment.CompanyId, appointment.ProfessionalId, appointment.StartAtUtc }).HasDatabaseName("IX_Appointments_CompanyId_ProfessionalId_StartAtUtc");
        builder.HasIndex(appointment => new { appointment.CompanyId, appointment.WorkOrderId, appointment.StartAtUtc }).HasDatabaseName("IX_Appointments_CompanyId_WorkOrderId_StartAtUtc");
        builder.HasOne<Professional>().WithMany().HasForeignKey(appointment => new { appointment.CompanyId, appointment.ProfessionalId }).HasPrincipalKey(professional => new { professional.CompanyId, professional.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkOrder>().WithMany().HasForeignKey(appointment => new { appointment.CompanyId, appointment.WorkOrderId }).HasPrincipalKey(workOrder => new { workOrder.CompanyId, workOrder.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(appointment => appointment.RescheduleHistory).WithOne().HasForeignKey(history => new { history.CompanyId, history.AppointmentId }).HasPrincipalKey(appointment => new { appointment.CompanyId, appointment.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(appointment => appointment.RescheduleHistory).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
