using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.MaintenanceOpportunities;

public sealed class MarkMaintenanceOpportunityAsContactedUseCase(MaintenanceOpportunityActionExecutor executor)
{
    public Task<OperationResult> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(opportunityId, status => status is MaintenanceOpportunityStatus.Pending or MaintenanceOpportunityStatus.Contacted, (opportunity, now) => opportunity.MarkAsContacted(now), cancellationToken);
}

public sealed class MarkMaintenanceOpportunityAsBookingRequestedUseCase(MaintenanceOpportunityActionExecutor executor)
{
    public Task<OperationResult> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(opportunityId, status => status is MaintenanceOpportunityStatus.Pending or MaintenanceOpportunityStatus.Contacted, (opportunity, now) => opportunity.MarkAsBookingRequested(now), cancellationToken);
}

public sealed class MarkMaintenanceOpportunityAsBookedUseCase(MaintenanceOpportunityActionExecutor executor)
{
    public Task<OperationResult> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(opportunityId, status => status == MaintenanceOpportunityStatus.BookingRequested, (opportunity, now) => opportunity.MarkAsBooked(now), cancellationToken);
}

public sealed class CompleteMaintenanceOpportunityUseCase(MaintenanceOpportunityActionExecutor executor)
{
    public Task<OperationResult> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(opportunityId, status => status == MaintenanceOpportunityStatus.Booked, (opportunity, now) => opportunity.Complete(now), cancellationToken);
}

public sealed class DismissMaintenanceOpportunityUseCase(MaintenanceOpportunityActionExecutor executor)
{
    public Task<OperationResult> ExecuteAsync(Guid opportunityId, CancellationToken cancellationToken = default) =>
        executor.ExecuteAsync(opportunityId, status => status is not MaintenanceOpportunityStatus.Completed and not MaintenanceOpportunityStatus.Dismissed, (opportunity, now) => opportunity.Dismiss(now), cancellationToken);
}

public sealed class MaintenanceOpportunityActionExecutor(
    IMaintenanceOpportunityRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(
        Guid opportunityId,
        Func<MaintenanceOpportunityStatus, bool> canTransition,
        Action<MaintenanceOpportunity, DateTimeOffset> transition,
        CancellationToken cancellationToken)
    {
        if (opportunityId == Guid.Empty)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.InvalidInput);
        }

        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var opportunity = await repository.GetForCompanyAsync(opportunityId, companyId.Value, cancellationToken);
        if (opportunity is null)
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.NotFound);
        }

        if (!canTransition(opportunity.Status))
        {
            return OperationResult.Failure(MaintenanceOpportunityErrors.InvalidStatus);
        }

        transition(opportunity, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult.Success();
    }
}
