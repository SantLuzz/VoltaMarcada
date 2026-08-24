using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Storage;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed class RemoveWorkOrderPhotoUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        var photo = order.Photos.SingleOrDefault(x => x.Id == photoId); if (photo is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        order.RemovePhoto(photoId, timeProvider.GetUtcNow()); await repository.SaveChangesAsync(cancellationToken); await fileStorage.DeleteAsync(photo.FileKey, cancellationToken); return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }
}
