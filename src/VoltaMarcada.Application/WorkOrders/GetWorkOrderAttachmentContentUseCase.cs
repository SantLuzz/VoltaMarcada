using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class GetWorkOrderAttachmentContentUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization)
{
    public async Task<FileStorageDownloadResult?> ExecuteAsync(Guid workOrderId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        var attachment = order?.Attachments.SingleOrDefault(x => x.Id == attachmentId);
        return attachment is null ? null : await fileStorage.GetAsync(attachment.FileKey, cancellationToken);
    }
}
