using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class WorkOrderChargeInstallment : ICompanyScopedEntity
{
    private WorkOrderChargeInstallment()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WorkOrderChargeId { get; private set; }
    public PaymentMethod? PaymentMethod { get; private set; }
    public decimal Amount { get; private set; }
    public string? OtherPaymentMethodDescription { get; private set; }
    public WorkOrderChargeInstallmentStatus Status { get; private set; }
    public DateTimeOffset? PaidAtUtc { get; private set; }
    public Guid? ConfirmedByUserId { get; private set; }
    public PaymentConfirmationType? ConfirmationType { get; private set; }
    public Guid? UpdatedByUserId { get; private set; }
    public DateTimeOffset? UpdatedAtUtc { get; private set; }
    public Guid? DeletedByUserId { get; private set; }
    public DateTimeOffset? DeletedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static WorkOrderChargeInstallment Create(Guid companyId, Guid workOrderChargeId, Guid id, PaymentMethod? paymentMethod, decimal amount, string? otherPaymentMethodDescription, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || workOrderChargeId == Guid.Empty || id == Guid.Empty) throw new DomainException("Charge installment identifiers are required.");
        if (paymentMethod is { } method && !Enum.IsDefined(method)) throw new DomainException("Payment method is invalid.");
        if (amount <= 0) throw new DomainException("Charge installment amount must be positive.");
        var description = string.IsNullOrWhiteSpace(otherPaymentMethodDescription) ? null : otherPaymentMethodDescription.Trim();
        if (paymentMethod == global::VoltaMarcada.Domain.Enums.PaymentMethod.Other && description is null) throw new DomainException("Other payment method description is required.");
        if (paymentMethod is not global::VoltaMarcada.Domain.Enums.PaymentMethod.Other && description is not null) description = null;
        return new WorkOrderChargeInstallment { Id = id, CompanyId = companyId, WorkOrderChargeId = workOrderChargeId, PaymentMethod = paymentMethod, Amount = amount, OtherPaymentMethodDescription = description, Status = WorkOrderChargeInstallmentStatus.Pending, CreatedAtUtc = now.ToUniversalTime() };
    }

    public void Confirm(Guid confirmedByUserId, DateTimeOffset now)
    {
        if (PaymentMethod is not { } paymentMethod) throw new DomainException("Payment method is required before confirmation.");
        Confirm(confirmedByUserId, paymentMethod, OtherPaymentMethodDescription, now);
    }

    public void Confirm(Guid confirmedByUserId, PaymentMethod paymentMethod, string? otherPaymentMethodDescription, DateTimeOffset now)
    {
        if (confirmedByUserId == Guid.Empty) throw new DomainException("Confirmation user ID is required.");
        if (Status != WorkOrderChargeInstallmentStatus.Pending) throw new DomainException("Only a pending charge installment can be confirmed.");
        ApplyPaymentDetails(paymentMethod, otherPaymentMethodDescription);
        Status = WorkOrderChargeInstallmentStatus.Paid;
        ConfirmedByUserId = confirmedByUserId;
        ConfirmationType = PaymentConfirmationType.Manual;
        PaidAtUtc = now.ToUniversalTime();
    }

    public void Update(decimal amount, PaymentMethod paymentMethod, string? otherPaymentMethodDescription, Guid updatedByUserId, DateTimeOffset now)
    {
        if (Status == WorkOrderChargeInstallmentStatus.Deleted) throw new DomainException("A deleted charge installment cannot be updated.");
        if (updatedByUserId == Guid.Empty) throw new DomainException("Update user ID is required.");
        if (amount <= 0) throw new DomainException("Charge installment amount must be positive.");
        ApplyPaymentDetails(paymentMethod, otherPaymentMethodDescription);
        Amount = amount;
        UpdatedByUserId = updatedByUserId;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Delete(Guid deletedByUserId, DateTimeOffset now)
    {
        if (Status == WorkOrderChargeInstallmentStatus.Deleted) throw new DomainException("Charge installment is already deleted.");
        if (deletedByUserId == Guid.Empty) throw new DomainException("Deletion user ID is required.");
        Status = WorkOrderChargeInstallmentStatus.Deleted;
        DeletedByUserId = deletedByUserId;
        DeletedAtUtc = now.ToUniversalTime();
    }

    private void ApplyPaymentDetails(PaymentMethod paymentMethod, string? otherPaymentMethodDescription)
    {
        if (!Enum.IsDefined(paymentMethod)) throw new DomainException("Payment method is invalid.");
        var description = string.IsNullOrWhiteSpace(otherPaymentMethodDescription) ? null : otherPaymentMethodDescription.Trim();
        if (paymentMethod == global::VoltaMarcada.Domain.Enums.PaymentMethod.Other && description is null) throw new DomainException("Other payment method description is required.");
        PaymentMethod = paymentMethod;
        OtherPaymentMethodDescription = paymentMethod == global::VoltaMarcada.Domain.Enums.PaymentMethod.Other ? description : null;
    }
}