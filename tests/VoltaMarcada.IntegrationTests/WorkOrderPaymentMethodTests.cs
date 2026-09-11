using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderPaymentMethodTests
{
    [Fact]
    public void CreateCharge_WhenOtherPaymentMethodHasNoDescription_ThrowsDomainException()
    {
        Assert.Throws<DomainException>(() => WorkOrderCharge.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            100m,
            [new WorkOrderChargeInstallmentDefinition(PaymentMethod.Other, 100m, null)],
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero)));
    }
}
