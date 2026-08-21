using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Storage;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed class RemoveWorkOrderPhotoUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<WorkOrderOperationResult> ExecuteAsync(Guid workOrderId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidStatus);
        var photo = order.Photos.SingleOrDefault(x => x.Id == photoId); if (photo is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.NotFound);
        order.RemovePhoto(photoId, timeProvider.GetUtcNow()); await repository.SaveChangesAsync(cancellationToken); await fileStorage.DeleteAsync(photo.FileKey, cancellationToken); return WorkOrderOperationResult.Success(order.Id);
    }
}
