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
    public Guid? WorkOrderId { get; private set; }
    public decimal TotalAmount { get; private set; }
    public DateOnly BusinessDate { get; private set; }
    public bool IsManualEntry { get; private set; }
    public Guid? CreatedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public IReadOnlyCollection<WorkOrderChargeInstallmentDefinition> Installments => installments.AsReadOnly();

    public static WorkOrderCharge Create(
        Guid companyId,
        Guid workOrderId,
        decimal totalAmount,
        IReadOnlyCollection<WorkOrderChargeInstallmentDefinition> installments,
        DateTimeOffset now) =>
        CreateInternal(companyId, workOrderId, totalAmount, installments, DateOnly.FromDateTime(now.UtcDateTime), false, null, now);

    public static WorkOrderCharge CreateAutomatic(
        Guid companyId,
        Guid workOrderId,
        decimal totalAmount,
        DateOnly businessDate,
        DateTimeOffset now) =>
        CreateInternal(companyId, workOrderId, totalAmount, [new WorkOrderChargeInstallmentDefinition(null, totalAmount, null)], businessDate, false, null, now);

    public static WorkOrderCharge CreateManual(
        Guid companyId,
        Guid? workOrderId,
        decimal amount,
        PaymentMethod paymentMethod,
        string? otherPaymentMethodDescription,
        DateOnly businessDate,
        Guid createdByUserId,
        DateTimeOffset now) =>
        CreateInternal(companyId, workOrderId, amount, [new WorkOrderChargeInstallmentDefinition(paymentMethod, amount, otherPaymentMethodDescription)], businessDate, true, createdByUserId, now);

    private static WorkOrderCharge CreateInternal(
        Guid companyId,
        Guid? workOrderId,
        decimal totalAmount,
        IReadOnlyCollection<WorkOrderChargeInstallmentDefinition> installments,
        DateOnly businessDate,
        bool isManualEntry,
        Guid? createdByUserId,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty) throw new DomainException("Company ID is required.");
        if (workOrderId == Guid.Empty) throw new DomainException("Work order ID is invalid.");
        if (totalAmount <= 0) throw new DomainException("Charge total must be positive.");
        if (installments is null || installments.Count == 0) throw new DomainException("Charge installments are required.");
        if (installments.Any(installment => installment.Amount <= 0)) throw new DomainException("Charge installment amount must be positive.");
        if (installments.Any(installment => installment.PaymentMethod == PaymentMethod.Other && string.IsNullOrWhiteSpace(installment.OtherPaymentMethodDescription))) throw new DomainException("Other payment method description is required.");
        if (installments.Sum(installment => installment.Amount) != totalAmount) throw new DomainException("Charge installments must match the charge total.");
        if (isManualEntry && (createdByUserId is null || createdByUserId == Guid.Empty)) throw new DomainException("Manual cash entry creator is required.");
        if (isManualEntry && installments.Any(installment => installment.PaymentMethod is null)) throw new DomainException("Manual cash entry payment method is required.");

        var charge = new WorkOrderCharge
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            WorkOrderId = workOrderId,
            TotalAmount = totalAmount,
            BusinessDate = businessDate,
            IsManualEntry = isManualEntry,
            CreatedByUserId = createdByUserId,
            CreatedAtUtc = now.ToUniversalTime()
        };

        charge.installments.AddRange(installments);
        return charge;
    }
}