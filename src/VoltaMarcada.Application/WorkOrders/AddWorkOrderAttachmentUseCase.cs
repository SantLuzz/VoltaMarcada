using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed record WorkOrderAttachmentUpload(Stream Content, string FileName, string ContentType, long Length, string? Description);

public sealed class AddWorkOrderAttachmentUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization, ICurrentUser currentUser, TimeProvider timeProvider)
{
    private const long MaximumAttachmentLength = 10 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png", "image/webp", "image/heic"];

    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, WorkOrderAttachmentUpload upload, CancellationToken cancellationToken = default)
    {
        if (upload is null || upload.Length is <= 0 or > MaximumAttachmentLength || string.IsNullOrWhiteSpace(upload.FileName) || !AllowedContentTypes.Contains(upload.ContentType, StringComparer.OrdinalIgnoreCase)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidAttachment);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        var stored = await fileStorage.UploadAsync(new FileStorageUploadRequest(new FileUpload(upload.Content, upload.FileName, upload.ContentType, upload.Length), $"companies/{scope.CompanyId:N}/work-orders/{workOrderId:N}/attachments"), cancellationToken);
        try
        {
            var attachment = order.AddAttachment(stored.Key, upload.FileName, upload.ContentType, upload.Length, upload.Description, currentUser.UserId, timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken);
            return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id, attachment.Id));
        }
        catch { await fileStorage.DeleteAsync(stored.Key, cancellationToken); throw; }
    }
}
