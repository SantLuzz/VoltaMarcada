using ServiceSync.Application.Abstractions.Persistence;

namespace ServiceSync.Application.WorkOrders;

public sealed class CompleteWorkOrderUseCase(IWorkOrderRepository repository, IAppointmentRepository appointmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<WorkOrderOperationResult> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.NotFound);
        var service = await serviceRepository.GetForCompanyAsync(order.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ServiceNotFound);
        if (order.ProfessionalId is not { } professionalId) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotQualified);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != order.ServiceId)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotQualified);
        if (order.Status != global::ServiceSync.Domain.Enums.WorkOrderStatus.InProgress) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidStatus);
        if (await appointmentRepository.HasOpenAppointmentsAsync(scope.CompanyId, order.Id, cancellationToken)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.OpenAppointments);
        if (order.ChecklistItems.Any(item => item.IsRequired && !item.IsCompleted)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ChecklistIncomplete);
        if (service.RequireBeforePhotos && order.Photos.All(photo => photo.Type != global::ServiceSync.Domain.Enums.WorkOrderPhotoType.Before)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.BeforePhotoRequired);
        if (service.RequireAfterPhotos && order.Photos.All(photo => photo.Type != global::ServiceSync.Domain.Enums.WorkOrderPhotoType.After)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.AfterPhotoRequired);
        order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken); return WorkOrderOperationResult.Success(order.Id);
    }
}
