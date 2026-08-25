using FluentValidation;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class UpdateWorkOrderAdditionalItemsUseCase(
    IWorkOrderRepository workOrderRepository,
    WorkOrderAuthorization authorization,
    IValidator<UpdateWorkOrderAdditionalItemsCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(
        Guid workOrderId,
        UpdateWorkOrderAdditionalItemsCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        }

        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var workOrder = await workOrderRepository.GetForAccessAsync(
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

        var additionalItems = command.AdditionalItems
            .Select(item => new WorkOrderItemDefinition(
                item.Description,
                item.Quantity,
                item.UnitPrice,
                WorkOrderItemType.Additional))
            .ToArray();

        workOrder.UpdateAdditionalItems(additionalItems, timeProvider.GetUtcNow());
        await workOrderRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(workOrder.Id));
    }
}
