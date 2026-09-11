using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IWorkOrderChargeRepository
{
    Task<WorkOrderCharge?> GetForWorkOrderAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default);

    Task<WorkOrderChargeInstallment?> GetInstallmentAsync(Guid companyId, Guid installmentId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<WorkOrderChargeInstallment>> GetInstallmentsForWorkOrderAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default);

    Task AddAsync(WorkOrderCharge charge, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}


