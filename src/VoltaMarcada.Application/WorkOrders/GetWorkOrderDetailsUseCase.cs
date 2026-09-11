using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class GetWorkOrderDetailsUseCase(IWorkOrderRepository repository, WorkOrderAuthorization authorization, IServiceRepository serviceRepository, ICompanyRepository companyRepository, IWorkOrderChargeRepository chargeRepository, ICustomerRepository customerRepository)
{
    public async Task<WorkOrderDetailDto?> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var workOrder = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (workOrder is null) return null;
        var service = await serviceRepository.GetForCompanyAsync(workOrder.ServiceId, scope.CompanyId, cancellationToken);
        var company = await companyRepository.GetByIdAsync(scope.CompanyId, cancellationToken);
        if (company is null) return null;
        if (service is null) return null;
        var canEdit = workOrder.Status == WorkOrderStatus.Pending;
        var canExecute = workOrder.Status == WorkOrderStatus.InProgress;
        var canManageAdditionalItems = workOrder.Status is not (WorkOrderStatus.Completed or WorkOrderStatus.Cancelled);
        var customer = await customerRepository.GetForCompanyAsync(workOrder.CustomerId, scope.CompanyId, cancellationToken);
        if (customer is null) return null;
        var installments = await chargeRepository.GetInstallmentsForWorkOrderAsync(scope.CompanyId, workOrder.Id, cancellationToken);
        var actorNames = await repository.GetHistoryActorNamesAsync(scope.CompanyId, workOrder.HistoryEntries.Where(entry => entry.ActorUserId is not null).Select(entry => entry.ActorUserId!.Value).Distinct().ToArray(), cancellationToken);
        return new WorkOrderDetailDto(workOrder.Id, workOrder.CustomerId, workOrder.EquipmentId, workOrder.ServiceId, workOrder.ProfessionalId, workOrder.Status, workOrder.Priority, workOrder.ReportedProblem, workOrder.ExecutionNotes, service.RequireBeforePhotos, service.RequireAfterPhotos, workOrder.DiscountAmount, workOrder.TotalAmount,
            workOrder.Items.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderItemDto(x.Id, x.Description, x.Quantity, x.UnitPrice, x.Type, x.TotalAmount, x.DisplayOrder)).ToArray(),
            workOrder.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDto(x.Id, x.Description, x.IsRequired, x.IsCompleted, x.CompletedAtUtc, x.DisplayOrder)).ToArray(),
            workOrder.Photos.OrderBy(x => x.CreatedAtUtc).Select(x => new WorkOrderPhotoDto(x.Id, x.Type, x.Description, x.CreatedAtUtc)).ToArray(),
            workOrder.Attachments.OrderBy(x => x.CreatedAtUtc).Select(x => new WorkOrderAttachmentDto(x.Id, x.OriginalFileName, x.ContentType, x.Length, x.Description, x.CreatedAtUtc)).ToArray(),
            workOrder.HistoryEntries.OrderBy(x => x.CreatedAtUtc).Select(x => new WorkOrderHistoryEntryDto(x.EventType, x.Description, x.ActorUserId is { } actorUserId && actorNames.TryGetValue(actorUserId, out var actorName) ? actorName : null, x.CreatedAtUtc)).ToArray(),
            new WorkOrderCapabilities(canEdit, scope.IsOwner && canEdit, workOrder.Status == WorkOrderStatus.Pending && workOrder.ProfessionalId is not null, canExecute, canManageAdditionalItems, scope.IsOwner), company.PaymentControlEnabled, installments.Select(item => new WorkOrderChargeInstallmentDto(item.Id, item.PaymentMethod, item.Amount, item.OtherPaymentMethodDescription, item.Status, item.PaidAtUtc)).ToArray(), customer.WhatsApp);
    }
}





