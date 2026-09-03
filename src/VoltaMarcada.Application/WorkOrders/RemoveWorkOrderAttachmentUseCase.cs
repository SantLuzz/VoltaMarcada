using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class RemoveWorkOrderAttachmentUseCase(IWorkOrderRepository repository, IFileStorage fileStorage, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(Guid workOrderId, Guid attachmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        if (order.Status != WorkOrderStatus.InProgress) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidStatus);
        var attachment = order.Attachments.SingleOrDefault(x => x.Id == attachmentId); if (attachment is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.NotFound);
        order.RemoveAttachment(attachmentId, timeProvider.GetUtcNow()); await repository.SaveChangesAsync(cancellationToken); await fileStorage.DeleteAsync(attachment.FileKey, cancellationToken);
        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(order.Id));
    }
}
