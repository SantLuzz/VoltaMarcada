using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed class CancelWorkOrderUseCase(
    IWorkOrderRepository repository,
    WorkOrderAuthorization authorization,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(
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
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        }

        if (workOrder.Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        }

        workOrder.Cancel(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(workOrder.Id));
    }
}
