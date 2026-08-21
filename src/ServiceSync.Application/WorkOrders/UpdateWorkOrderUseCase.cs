using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.WorkOrders;

public sealed class UpdateWorkOrderUseCase(
    IWorkOrderRepository workOrderRepository,
    ICustomerRepository customerRepository,
    IEquipmentRepository equipmentRepository,
    IServiceRepository serviceRepository,
    IProfessionalRepository professionalRepository,
    WorkOrderAuthorization authorization,
    IValidator<UpdateWorkOrderCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<WorkOrderOperationResult> ExecuteAsync(
        Guid workOrderId,
        UpdateWorkOrderCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidInput);
        }

        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var workOrder = await workOrderRepository.GetForAccessAsync(
            workOrderId,
            scope.CompanyId,
            scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty,
            cancellationToken);

        if (workOrder is null)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.NotFound);
        }

        if (workOrder.Status != WorkOrderStatus.Pending)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidStatus);
        }

        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken);
        if (customer is null)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.CustomerNotFound);
        }

        var equipment = await equipmentRepository.GetForCompanyAsync(command.EquipmentId, scope.CompanyId, cancellationToken);
        if (equipment is null)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.EquipmentNotFound);
        }

        if (equipment.CustomerId != customer.Id)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidEquipmentCustomer);
        }

        var service = await serviceRepository.GetForCompanyAsync(command.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null || !service.IsActive)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ServiceNotFound);
        }

        var professionalId = scope.IsOwner ? command.ProfessionalId : workOrder.ProfessionalId;
        if (!scope.IsOwner && professionalId != scope.ProfessionalId)
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.Forbidden);
        }

        if (professionalId is { } id)
        {
            var professional = await professionalRepository.GetForCompanyAsync(id, scope.CompanyId, cancellationToken);
            if (professional is null)
            {
                return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotFound);
            }

            if (!professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != service.Id))
            {
                return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotQualified);
            }
        }

        var checklistItems = service.ChecklistItems
            .OrderBy(x => x.DisplayOrder)
            .Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder))
            .ToArray();
        if (!WorkOrderFinancialItems.TryCreate(service, command.ServiceUnitPrice, command.AdditionalItems, out var items))
        {
            return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidInput);
        }

        workOrder.UpdatePendingDetails(
            customer.Id,
            equipment.Id,
            service.Id,
            professionalId,
            command.Priority,
            command.ReportedProblem,
            items,
            checklistItems,
            timeProvider.GetUtcNow());

        await workOrderRepository.SaveChangesAsync(cancellationToken);
        return WorkOrderOperationResult.Success(workOrder.Id);
    }
}
