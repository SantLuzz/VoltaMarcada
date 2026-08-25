using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class GetWorkOrderDetailsUseCase(IWorkOrderRepository repository, WorkOrderAuthorization authorization, IServiceRepository serviceRepository)
{
    public async Task<WorkOrderDetailDto?> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var workOrder = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (workOrder is null) return null;
        var service = await serviceRepository.GetForCompanyAsync(workOrder.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null) return null;
        var canEdit = workOrder.Status == WorkOrderStatus.Pending;
        var canExecute = workOrder.Status == WorkOrderStatus.InProgress;
        return new WorkOrderDetailDto(workOrder.Id, workOrder.CustomerId, workOrder.EquipmentId, workOrder.ServiceId, workOrder.ProfessionalId, workOrder.Status, workOrder.Priority, workOrder.ReportedProblem, workOrder.ExecutionNotes, service.RequireBeforePhotos, service.RequireAfterPhotos, workOrder.TotalAmount,
            workOrder.Items.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderItemDto(x.Id, x.Description, x.Quantity, x.UnitPrice, x.Type, x.TotalAmount, x.DisplayOrder)).ToArray(),
            workOrder.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDto(x.Id, x.Description, x.IsRequired, x.IsCompleted, x.CompletedAtUtc, x.DisplayOrder)).ToArray(),
            workOrder.Photos.OrderBy(x => x.CreatedAtUtc).Select(x => new WorkOrderPhotoDto(x.Id, x.Type, x.Description, x.CreatedAtUtc)).ToArray(),
            new WorkOrderCapabilities(canEdit, scope.IsOwner && canEdit, workOrder.Status == WorkOrderStatus.Pending && workOrder.ProfessionalId is not null, canExecute));
    }
}
