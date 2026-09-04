using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class UpdateWorkOrderUseCase(
    IWorkOrderRepository workOrderRepository,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IServiceRepository serviceRepository,
    IProfessionalRepository professionalRepository,
    WorkOrderAuthorization authorization,
    ICurrentUser currentUser,
    IValidator<UpdateWorkOrderCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(
        Guid workOrderId,
        UpdateWorkOrderCommand command,
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

        if (workOrder.Status != WorkOrderStatus.Pending)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        }

        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken);
        if (customer is null)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.CustomerNotFound);
        }

        var equipment = await equipmentRepository.GetForCompanyAsync(command.EquipmentId, scope.CompanyId, cancellationToken);
        if (equipment is null)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.EquipmentNotFound);
        }

        if (equipment.CustomerId != customer.Id)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidEquipmentCustomer);
        }

        var service = await serviceRepository.GetForCompanyAsync(command.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ServiceNotFound);
        }

        var professionalId = scope.IsOwner ? command.ProfessionalId : workOrder.ProfessionalId;
        if (!scope.IsOwner && professionalId != scope.ProfessionalId)
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.Forbidden);
        }

        if (professionalId is { } id)
        {
            var professional = await professionalRepository.GetForCompanyAsync(id, scope.CompanyId, cancellationToken);
            if (professional is null)
            {
                return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotFound);
            }

            if (!professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != service.Id))
            {
                return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotQualified);
            }
        }

        var checklistItems = service.ChecklistItems
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder))
            .ToArray();
        if (!WorkOrderFinancialItems.TryCreate(service, command.ServiceUnitPrice, command.AdditionalItems, out var items))
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        }

        var now = timeProvider.GetUtcNow();
        var professionalChanged = workOrder.ProfessionalId != professionalId;
        workOrder.UpdatePendingDetails(
            customer.Id,
            equipment.Id,
            service.Id,
            professionalId,
            command.Priority,
            command.ReportedProblem,
            items,
            checklistItems,
            command.DiscountAmount,
            now);

        var actorUserId = currentUser.UserId == Guid.Empty ? (Guid?)null : currentUser.UserId;
        workOrder.RecordHistory(WorkOrderHistoryEventType.DetailsUpdated, actorUserId, WorkOrderHistoryDescriptions.DetailsUpdated(), now);
        if (professionalChanged)
        {
            workOrder.RecordHistory(WorkOrderHistoryEventType.ProfessionalAssigned, actorUserId, WorkOrderHistoryDescriptions.ProfessionalAssigned(), now);
        }

        await workOrderRepository.SaveChangesAsync(cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(workOrder.Id));
    }
}
