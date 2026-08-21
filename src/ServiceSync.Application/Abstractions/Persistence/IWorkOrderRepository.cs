using ServiceSync.Application.WorkOrders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetForAccessAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderListItem>> GetAllForAccessAsync(Guid companyId, Guid? professionalId, GetWorkOrdersQuery query, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
