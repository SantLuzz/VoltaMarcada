using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class PaymentConfirmationTests
{
    [Fact]
    public void Confirm_RecordsManualConfirmationType()
    {
        var installment = WorkOrderChargeInstallment.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), PaymentMethod.Pix, 100m, null, DateTimeOffset.UtcNow);

        installment.Confirm(Guid.NewGuid(), DateTimeOffset.UtcNow);

        Assert.Equal(PaymentConfirmationType.Manual, installment.ConfirmationType);
    }
}
