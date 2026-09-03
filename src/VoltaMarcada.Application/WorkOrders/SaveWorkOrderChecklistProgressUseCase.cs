using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class SaveWorkOrderChecklistProgressUseCase(IWorkOrderRepository repository, WorkOrderAuthorization authorization, ICurrentUser currentUser, IValidator<SaveWorkOrderChecklistProgressCommand> validator, TimeProvider timeProvider)
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
        var normalizedExecutionNotes = string.IsNullOrWhiteSpace(command.ExecutionNotes) ? null : command.ExecutionNotes.Trim();
        var executionNotesChanged = !string.Equals(order.ExecutionNotes, normalizedExecutionNotes, StringComparison.Ordinal);
        order.SaveChecklistProgress(command.ChecklistItems.ToDictionary(x => x.ChecklistItemId, x => x.IsCompleted), now);
        order.UpdateExecutionNotes(command.ExecutionNotes, now);
        var actorUserId = currentUser.UserId == Guid.Empty ? (Guid?)null : currentUser.UserId;
        order.RecordHistory(WorkOrderHistoryEventType.ChecklistProgressSaved, actorUserId, WorkOrderHistoryDescriptions.ChecklistProgressSaved(), now);
        if (executionNotesChanged)
        {
            order.RecordHistory(WorkOrderHistoryEventType.ExecutionNotesUpdated, actorUserId, WorkOrderHistoryDescriptions.ExecutionNotesUpdated(), now);
        }
        await repository.SaveChangesAsync(cancellationToken); return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }
}
