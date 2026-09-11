using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderPaymentDomainTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void CreateCharge_WhenInstallmentsDoNotMatchWorkOrderTotal_ThrowsDomainException()
    {
        var workOrder = CreateWorkOrder(100m);

        Assert.Throws<DomainException>(() => WorkOrderCharge.Create(
            workOrder.CompanyId,
            workOrder.Id,
            workOrder.TotalAmount,
            [new WorkOrderChargeInstallmentDefinition(PaymentMethod.Pix, 99m, null)],
            Now));
    }

    private static WorkOrder CreateWorkOrder(decimal amount) => WorkOrder.Create(
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        Guid.NewGuid(),
        WorkOrderPriority.Normal,
        null,
        [new WorkOrderItemDefinition("Maintenance", 1, amount, WorkOrderItemType.Service)],
        [],
        Now);
}
