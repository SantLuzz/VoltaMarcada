using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.WorkOrders;

public sealed record WorkOrderPaymentDecisionInput(
    IReadOnlyCollection<WorkOrderChargeInstallmentDefinition>? Installments,
    string? NoChargeReason);
