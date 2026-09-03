using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class RemoveWorkOrderPhotoUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        var photo = order.Photos.SingleOrDefault(x => x.Id == photoId); if (photo is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        var now = timeProvider.GetUtcNow(); order.RemovePhoto(photoId, now); order.RecordHistory(WorkOrderHistoryEventType.PhotoRemoved, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, WorkOrderHistoryDescriptions.PhotoRemoved(), now); await repository.SaveChangesAsync(cancellationToken); await fileStorage.DeleteAsync(photo.FileKey, cancellationToken); return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }
}
