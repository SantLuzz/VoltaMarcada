using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

public sealed class MaintenanceOpportunityOwnerAuthorization(WorkOrderAuthorization workOrderAuthorization)
{
    public async Task<Guid?> GetOwnerCompanyIdAsync(CancellationToken cancellationToken = default)
    {
        var scope = await workOrderAuthorization.GetAccessScopeAsync(cancellationToken);
        return scope.IsOwner ? scope.CompanyId : null;
    }
}
