using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Configurations;

public sealed class WorkOrderChargeInstallmentConfiguration : IEntityTypeConfiguration<WorkOrderChargeInstallment>
{
    public void Configure(EntityTypeBuilder<WorkOrderChargeInstallment> builder)
    {
        builder.ToTable("WorkOrderChargeInstallments");
        builder.HasKey(installment => installment.Id);
        builder.Property(installment => installment.Id).ValueGeneratedNever();
        builder.Property(installment => installment.CompanyId).IsRequired();
        builder.Property(installment => installment.WorkOrderChargeId).IsRequired();
        builder.Property(installment => installment.PaymentMethod).IsRequired(false);
        builder.Property(installment => installment.Amount).HasPrecision(18, 2).IsRequired();
        builder.Property(installment => installment.OtherPaymentMethodDescription).HasMaxLength(200).IsRequired(false);
        builder.Property(installment => installment.Status).IsRequired();
        builder.Property(installment => installment.ConfirmationType).IsRequired(false);
        builder.Property(installment => installment.UpdatedByUserId).IsRequired(false);
        builder.Property(installment => installment.UpdatedAtUtc).IsRequired(false);
        builder.Property(installment => installment.DeletedByUserId).IsRequired(false);
        builder.Property(installment => installment.DeletedAtUtc).IsRequired(false);
        builder.Property(installment => installment.CreatedAtUtc).IsRequired();
        builder.HasIndex(installment => new { installment.CompanyId, installment.Status, installment.PaidAtUtc }).HasDatabaseName("IX_WorkOrderChargeInstallments_CompanyId_Status_PaidAtUtc");
        builder.HasOne<WorkOrderCharge>().WithMany().HasForeignKey(installment => installment.WorkOrderChargeId).OnDelete(DeleteBehavior.Cascade);
        builder.HasOne<Company>().WithMany().HasForeignKey(installment => installment.CompanyId).OnDelete(DeleteBehavior.Restrict);
    }
}