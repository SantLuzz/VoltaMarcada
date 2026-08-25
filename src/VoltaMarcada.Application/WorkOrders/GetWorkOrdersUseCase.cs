using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class GetWorkOrdersUseCase(IWorkOrderRepository repository, WorkOrderAuthorization authorization)
{
    public async Task<IReadOnlyList<WorkOrderListItem>> ExecuteAsync(GetWorkOrdersQuery query, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken); return await repository.GetAllForAccessAsync(scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, query, cancellationToken);
    }
}
