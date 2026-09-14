using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Payments;

public sealed record GetCashEntriesQuery(DateOnly? BusinessDate, bool PendingOnly);
public sealed record CashSummaryDto(decimal TotalAmount, decimal PendingAmount, decimal ConfirmedAmount);
public sealed record CashEntryDto(Guid Id, Guid? WorkOrderId, DateOnly BusinessDate, decimal Amount, PaymentMethod? PaymentMethod, string? OtherPaymentMethodDescription, WorkOrderChargeInstallmentStatus Status, DateTimeOffset CreatedAtUtc, DateTimeOffset? ConfirmedAtUtc);
public sealed record CashEntriesDto(DateOnly? BusinessDate, bool PendingOnly, CashSummaryDto Summary, IReadOnlyList<CashEntryDto> Entries);
public sealed record CreateManualCashEntryCommand(Guid? WorkOrderId, decimal Amount, PaymentMethod PaymentMethod, string? OtherPaymentMethodDescription, DateOnly BusinessDate);
public sealed record ConfirmCashEntryCommand(PaymentMethod PaymentMethod, string? OtherPaymentMethodDescription);