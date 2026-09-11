using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChargeInstallmentManagementTests
{
    [Fact]
    public void UpdateAmount_WhenInstallmentWasPaid_KeepsConfirmationAndUpdatesAmount()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var companyId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var installment = WorkOrderChargeInstallment.Create(companyId, chargeId, Guid.NewGuid(), PaymentMethod.Pix, 100m, null, now);
        installment.Confirm(ownerId, now.AddMinutes(1));

        installment.Update(75m, PaymentMethod.CreditCard, null, ownerId, now.AddMinutes(2));

        Assert.Equal(75m, installment.Amount);
        Assert.Equal(PaymentMethod.CreditCard, installment.PaymentMethod);
        Assert.Equal(WorkOrderChargeInstallmentStatus.Paid, installment.Status);
        Assert.Equal(ownerId, installment.ConfirmedByUserId);
    }

    [Fact]
    public void Delete_WhenInstallmentWasPaid_MarksItDeletedAndPreservesConfirmation()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var companyId = Guid.NewGuid();
        var chargeId = Guid.NewGuid();
        var ownerId = Guid.NewGuid();
        var installment = WorkOrderChargeInstallment.Create(companyId, chargeId, Guid.NewGuid(), PaymentMethod.Pix, 100m, null, now);
        installment.Confirm(ownerId, now.AddMinutes(1));

        installment.Delete(ownerId, now.AddMinutes(2));

        Assert.Equal(WorkOrderChargeInstallmentStatus.Deleted, installment.Status);
        Assert.Equal(ownerId, installment.DeletedByUserId);
        Assert.Equal(now.AddMinutes(2), installment.DeletedAtUtc);
        Assert.Equal(ownerId, installment.ConfirmedByUserId);
    }
}
