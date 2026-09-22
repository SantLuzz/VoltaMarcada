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

    [Fact]
    public void CreateAutomaticCharge_WithSelectedPaymentMethod_PreservesThePendingInstallment()
    {
        var companyId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();

        var charge = WorkOrderCharge.CreateAutomatic(
            companyId,
            workOrderId,
            100m,
            [new WorkOrderChargeInstallmentDefinition(PaymentMethod.CreditCard, 100m, null)],
            new DateOnly(2026, 9, 10),
            Now);

        var installment = Assert.Single(charge.Installments);
        Assert.Equal(PaymentMethod.CreditCard, installment.PaymentMethod);
        Assert.Equal(100m, installment.Amount);
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
