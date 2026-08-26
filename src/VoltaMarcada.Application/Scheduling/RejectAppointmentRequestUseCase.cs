using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Scheduling;

public sealed class RejectAppointmentRequestUseCase(WorkOrderAuthorization authorization, IAppointmentRequestRepository repository, TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(Guid requestId, string? reason, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var request = await repository.GetForCompanyAsync(requestId, scope.CompanyId, cancellationToken);
        if (request is null) return OperationResult.Failure(SchedulingErrors.NotFound);
        if (!scope.IsOwner && scope.ProfessionalId != request.ProfessionalId) return OperationResult.Failure(SchedulingErrors.Forbidden);
        if (request.Status is not AppointmentRequestStatus.Pending and not AppointmentRequestStatus.RescheduleRequested) return OperationResult.Failure(SchedulingErrors.InvalidStatus);
        request.Reject(reason, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
