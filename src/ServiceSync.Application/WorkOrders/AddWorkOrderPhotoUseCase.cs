using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Storage;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderPhotoUpload(Stream Content, string FileName, string ContentType, long Length, WorkOrderPhotoType Type, string? Description);

public sealed class AddWorkOrderPhotoUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private const long MaximumPhotoLength = 10 * 1024 * 1024;
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, WorkOrderPhotoUpload upload, CancellationToken cancellationToken = default)
    {
        if (upload is null || !Enum.IsDefined(upload.Type) || upload.Length <= 0 || upload.Length > MaximumPhotoLength || !new[] { "image/jpeg", "image/png", "image/webp", "image/heic" }.Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidPhoto);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        if (order.Photos.Count >= 30 || order.Photos.Count(x => x.Type == upload.Type) >= 10) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.PhotoLimitReached);
        var stored = await fileStorage.UploadAsync(new FileStorageUploadRequest(new FileUpload(upload.Content, upload.FileName, upload.ContentType, upload.Length), $"companies/{scope.CompanyId:N}/work-orders/{workOrderId:N}"), cancellationToken);
        try { var photo = order.AddPhoto(upload.Type, stored.Key, upload.Description, currentUser.UserId, timeProvider.GetUtcNow()); await repository.SaveChangesAsync(cancellationToken); return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id, photo.Id)); }
        catch { await fileStorage.DeleteAsync(stored.Key, cancellationToken); throw; }
    }
}
