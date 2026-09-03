using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class CancelWorkOrderUseCase(
    IWorkOrderRepository repository,
    WorkOrderAuthorization authorization,
    ICurrentUser currentUser,
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

        var now = timeProvider.GetUtcNow();
        workOrder.Cancel(now);
        workOrder.RecordHistory(WorkOrderHistoryEventType.Cancelled, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, WorkOrderHistoryDescriptions.Cancelled(), now);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(workOrder.Id));
    }
}
