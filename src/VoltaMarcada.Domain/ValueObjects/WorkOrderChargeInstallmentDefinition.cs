using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Domain.ValueObjects;

public sealed record WorkOrderChargeInstallmentDefinition(
    PaymentMethod PaymentMethod,
    decimal Amount,
    string? OtherPaymentMethodDescription);
