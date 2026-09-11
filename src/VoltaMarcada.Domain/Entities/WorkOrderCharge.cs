using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class WorkOrderCharge : ICompanyScopedEntity
{
    private readonly List<WorkOrderChargeInstallmentDefinition> installments = [];

    private WorkOrderCharge()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid WorkOrderId { get; private set; }

    public decimal TotalAmount { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public IReadOnlyCollection<WorkOrderChargeInstallmentDefinition> Installments => installments.AsReadOnly();

    public static WorkOrderCharge Create(
        Guid companyId,
        Guid workOrderId,
        decimal totalAmount,
        IReadOnlyCollection<WorkOrderChargeInstallmentDefinition> installments,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty) throw new DomainException("Company ID is required.");
        if (workOrderId == Guid.Empty) throw new DomainException("Work order ID is required.");
        if (totalAmount <= 0) throw new DomainException("Charge total must be positive.");
        if (installments is null || installments.Count == 0) throw new DomainException("Charge installments are required.");
        if (installments.Any(installment => installment.Amount <= 0)) throw new DomainException("Charge installment amount must be positive.");
        if (installments.Any(installment => installment.PaymentMethod == PaymentMethod.Other && string.IsNullOrWhiteSpace(installment.OtherPaymentMethodDescription))) throw new DomainException("Other payment method description is required.");
        if (installments.Sum(installment => installment.Amount) != totalAmount) throw new DomainException("Charge installments must match the work order total.");

        var charge = new WorkOrderCharge
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkOrderId = workOrderId,
            TotalAmount = totalAmount,
            CreatedAtUtc = now.ToUniversalTime()
        };

        charge.installments.AddRange(installments);
        return charge;
    }
}
