using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class GetWorkOrderPhotoContentUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization)
{
    public async Task<FileStorageDownloadResult?> ExecuteAsync(Guid workOrderId, Guid photoId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        var photo = order?.Photos.SingleOrDefault(x => x.Id == photoId);
        return photo is null ? null : await fileStorage.GetAsync(photo.FileKey, cancellationToken);
    }
}
