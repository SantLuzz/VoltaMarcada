using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChargeInstallmentTests
{
    [Fact]
    public void Confirm_WhenPending_RecordsManualConfirmation()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var confirmerId = Guid.NewGuid();
        var installment = WorkOrderChargeInstallment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.Pix, 100m, null, now);

        installment.Confirm(confirmerId, now.AddMinutes(1));

        Assert.Equal(WorkOrderChargeInstallmentStatus.Paid, installment.Status);
        Assert.Equal(confirmerId, installment.ConfirmedByUserId);
        Assert.Equal(now.AddMinutes(1), installment.PaidAtUtc);
    }
}
