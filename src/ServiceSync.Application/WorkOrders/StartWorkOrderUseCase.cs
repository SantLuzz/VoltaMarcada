using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed class StartWorkOrderUseCase(IWorkOrderRepository repository, IProfessionalRepository professionalRepository, WorkOrderAuthorization authorization, TimeProvider timeProvider)
{
    public async Task<WorkOrderOperationResult> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var order = await repository.GetForAccessAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (order is null) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.NotFound);
        if (order.ProfessionalId is not { } professionalId) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotQualified);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != order.ServiceId)) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.ProfessionalNotQualified);
        if (order.Status != WorkOrderStatus.Pending) return WorkOrderOperationResult.Failure(WorkOrderOperationResultCode.InvalidStatus);
        order.Start(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken); return WorkOrderOperationResult.Success(order.Id);
    }
}
