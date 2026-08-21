using FluentValidation;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.WorkOrders;

public sealed class CreateWorkOrderUseCase(IWorkOrderRepository workOrderRepository, ICustomerRepository customerRepository, IEquipmentRepository equipmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, WorkOrderAuthorization authorization, ICurrentUser currentUser, IValidator<CreateWorkOrderCommand> validator, TimeProvider timeProvider)
{
    public async Task<WorkOrderOperationResult> ExecuteAsync(CreateWorkOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidInput);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken); if (customer is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.CustomerNotFound);
        var equipment = await equipmentRepository.GetForCompanyAsync(command.EquipmentId, scope.CompanyId, cancellationToken); if (equipment is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.EquipmentNotFound); if (equipment.CustomerId != customer.Id) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidEquipmentCustomer);
        var service = await serviceRepository.GetForCompanyAsync(command.ServiceId, scope.CompanyId, cancellationToken); if (service is null || !service.IsActive) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ServiceNotFound);
        Guid? professionalId = command.ProfessionalId;
        if (!scope.IsOwner) { if (scope.ProfessionalId is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalProfileRequired); professionalId = scope.ProfessionalId; }
        if (professionalId is { } id) { var professional = await professionalRepository.GetForCompanyAsync(id, scope.CompanyId, cancellationToken); if (professional is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotFound); if (!professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != service.Id)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotQualified); }
        if (!WorkOrderFinancialItems.TryCreate(service, command.ServiceUnitPrice, command.AdditionalItems, out var items)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidInput);
        var now = timeProvider.GetUtcNow();
        var workOrder = global::ServiceSync.Domain.Entities.WorkOrder.Create(scope.CompanyId, customer.Id, equipment.Id, service.Id, professionalId, currentUser.UserId, command.Priority, command.ReportedProblem, items, service.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder)).ToArray(), now);
        await workOrderRepository.AddAsync(workOrder, cancellationToken); await workOrderRepository.SaveChangesAsync(cancellationToken); return WorkOrderOperationResult.Success(workOrder.Id);
    }
}
