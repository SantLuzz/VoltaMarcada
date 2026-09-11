using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderChargeConfiguration : IEntityTypeConfiguration<WorkOrderCharge>
{
    public void Configure(EntityTypeBuilder<WorkOrderCharge> builder)
    {
        builder.ToTable("WorkOrderCharges");
        builder.HasKey(charge => charge.Id);
        builder.Property(charge => charge.Id).ValueGeneratedNever();
        builder.Property(charge => charge.CompanyId).IsRequired();
        builder.Property(charge => charge.WorkOrderId).IsRequired();
        builder.Property(charge => charge.TotalAmount).HasPrecision(18, 2).IsRequired();
        builder.Property(charge => charge.CreatedAtUtc).IsRequired();
        builder.Ignore(charge => charge.Installments);
        builder.HasIndex(charge => charge.WorkOrderId).IsUnique().HasDatabaseName("UX_WorkOrderCharges_WorkOrderId");
        builder.HasOne<Company>().WithMany().HasForeignKey(charge => charge.CompanyId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<WorkOrder>().WithMany().HasForeignKey(charge => new { charge.CompanyId, charge.WorkOrderId }).HasPrincipalKey(order => new { order.CompanyId, order.Id }).OnDelete(DeleteBehavior.Cascade);
    }
}
