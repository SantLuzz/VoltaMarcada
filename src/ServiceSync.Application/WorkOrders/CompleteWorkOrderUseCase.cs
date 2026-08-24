using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;

namespace ServiceSync.Application.WorkOrders;

public sealed class CompleteWorkOrderUseCase(IWorkOrderRepository repository, IAppointmentRepository appointmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        var service = await serviceRepository.GetForCompanyAsync(order.ServiceId, scope.CompanyId, cancellationToken);
        if (service is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ServiceNotFound);
        if (order.ProfessionalId is not { } professionalId) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotQualified);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != order.ServiceId)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotQualified);
        if (order.Status != global::ServiceSync.Domain.Enums.WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        if (await appointmentRepository.HasOpenAppointmentsAsync(scope.CompanyId, order.Id, cancellationToken)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.OpenAppointments);
        if (order.ChecklistItems.Any(item => item.IsRequired && !item.IsCompleted)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ChecklistIncomplete);
        if (service.RequireBeforePhotos && order.Photos.All(photo => photo.Type != global::ServiceSync.Domain.Enums.WorkOrderPhotoType.Before)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.BeforePhotoRequired);
        if (service.RequireAfterPhotos && order.Photos.All(photo => photo.Type != global::ServiceSync.Domain.Enums.WorkOrderPhotoType.After)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.AfterPhotoRequired);
        order.Complete(service.RequireBeforePhotos, service.RequireAfterPhotos, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken); return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }
}
