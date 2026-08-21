using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed class CancelWorkOrderUseCase(
    IWorkOrderRepository repository,
    WorkOrderAuthorization authorization,
    TimeProvider timeProvider)
{
    public async Task<WorkOrderOperationResult> ExecuteAsync(
        Guid workOrderId,
        CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var workOrder = await repository.GetForAccessAsync(
            workOrderId,
            scope.CompanyId,
            scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty,
            cancellationToken);

        if (workOrder is null)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.NotFound);
        }

        if (workOrder.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidStatus);
        }

        workOrder.Cancel(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return WorkOrderOperationResult.Success(workOrder.Id);
    }
}
