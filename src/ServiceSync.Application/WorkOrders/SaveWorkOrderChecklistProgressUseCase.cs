using ServiceSync.Application.Abstractions.Operations;
using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed class SaveWorkOrderChecklistProgressUseCase(IWorkOrderRepository repository, WorkOrderAuthorization authorization, IValidator<SaveWorkOrderChecklistProgressCommand> validator, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, SaveWorkOrderChecklistProgressCommand command, CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        if (command.ChecklistItems.Any(input => order.ChecklistItems.All(item => item.Id != input.ChecklistItemId))) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        var now = timeProvider.GetUtcNow();
        order.SaveChecklistProgress(command.ChecklistItems.ToDictionary(x => x.ChecklistItemId, x => x.IsCompleted), now);
        order.UpdateExecutionNotes(command.ExecutionNotes, now);
        await repository.SaveChangesAsync(cancellationToken); return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }
}
