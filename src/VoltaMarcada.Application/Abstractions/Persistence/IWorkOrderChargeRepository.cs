using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public sealed record CashEntryPersistenceModel(
    Guid InstallmentId,
    Guid? WorkOrderId,
    DateOnly BusinessDate,
    decimal Amount,
    PaymentMethod? PaymentMethod,
    string? OtherPaymentMethodDescription,
    WorkOrderChargeInstallmentStatus Status,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? PaidAtUtc);

public interface IWorkOrderChargeRepository
{
    Task<WorkOrderCharge?> GetForWorkOrderAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default);
    Task<WorkOrderChargeInstallment?> GetInstallmentAsync(Guid companyId, Guid installmentId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderChargeInstallment>> GetInstallmentsForWorkOrderAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashEntryPersistenceModel>> GetDailyEntriesAsync(Guid companyId, DateOnly businessDate, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CashEntryPersistenceModel>> GetPendingEntriesAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task AddAsync(WorkOrderCharge charge, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}