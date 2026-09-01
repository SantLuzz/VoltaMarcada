using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderConfiguration : IEntityTypeConfiguration<WorkOrder>
{
    public void Configure(EntityTypeBuilder<WorkOrder> builder)
    {
        builder.ToTable("WorkOrders"); builder.HasKey(x => x.Id); builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.CompanyId).IsRequired(); builder.Property(x => x.CustomerId).IsRequired(); builder.Property(x => x.EquipmentId).IsRequired(); builder.Property(x => x.ServiceId).IsRequired(); builder.Property(x => x.CreatedByUserId).IsRequired();
        builder.Property(x => x.ReportedProblem).HasMaxLength(2000); builder.Property(x => x.ExecutionNotes).HasMaxLength(4000);
        builder.HasAlternateKey(x => new { x.CompanyId, x.Id }).HasName("AK_WorkOrders_CompanyId_Id");
        builder.HasIndex(x => new { x.CompanyId, x.Status, x.CreatedAtUtc }).HasDatabaseName("IX_WorkOrders_CompanyId_Status_CreatedAtUtc");
        builder.HasIndex(x => new { x.CompanyId, x.ProfessionalId, x.CreatedAtUtc }).HasDatabaseName("IX_WorkOrders_CompanyId_ProfessionalId_CreatedAtUtc");
        builder.HasIndex(x => x.SourceQuoteId).IsUnique().HasFilter("\"SourceQuoteId\" IS NOT NULL");
        builder.HasIndex(x => new { x.CompanyId, x.SourceAppointmentId });
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => new { x.CompanyId, x.CustomerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Equipment>().WithMany().HasForeignKey(x => new { x.CompanyId, x.EquipmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Service>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ServiceId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Professional>().WithMany().HasForeignKey(x => new { x.CompanyId, x.ProfessionalId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Appointment>().WithMany().HasForeignKey(x => new { x.CompanyId, x.SourceAppointmentId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasMany(x => x.Items).WithOne().HasForeignKey(x => new { x.CompanyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.ChecklistItems).WithOne().HasForeignKey(x => new { x.CompanyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(x => x.Photos).WithOne().HasForeignKey(x => new { x.CompanyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Items).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.ChecklistItems).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(x => x.Photos).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
