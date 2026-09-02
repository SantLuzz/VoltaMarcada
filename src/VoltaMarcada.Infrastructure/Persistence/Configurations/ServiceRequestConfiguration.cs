using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.ProblemDescription).HasMaxLength(2000);
        builder.HasAlternateKey(x => new { x.CompanyId, x.Id });
        builder.HasIndex(x => new { x.CompanyId, x.Status, x.CreatedAtUtc });
        builder.HasIndex(x => new { x.CompanyId, x.AppointmentRequestId }).IsUnique();
        builder.HasIndex(x => new { x.CompanyId, x.WorkOrderId }).IsUnique().HasFilter("\"WorkOrderId\" IS NOT NULL");
        builder.HasOne<Company>().WithMany().HasForeignKey(x => x.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Customer>().WithMany().HasForeignKey(x => new { x.CompanyId, x.CustomerId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<AppointmentRequest>().WithMany().HasForeignKey(x => new { x.CompanyId, x.AppointmentRequestId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkOrder>().WithMany().HasForeignKey(x => new { x.CompanyId, x.WorkOrderId }).HasPrincipalKey(x => new { x.CompanyId, x.Id }).OnDelete(DeleteBehavior.Restrict);
    }
}
