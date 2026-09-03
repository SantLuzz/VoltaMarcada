using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IWorkOrderRepository
{
    Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default);
    Task<WorkOrder?> GetForAccessAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<Guid, string>> GetHistoryActorNamesAsync(Guid companyId, IReadOnlyCollection<Guid> userIds, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<WorkOrderListItem>> GetAllForAccessAsync(Guid companyId, Guid? professionalId, GetWorkOrdersQuery query, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
