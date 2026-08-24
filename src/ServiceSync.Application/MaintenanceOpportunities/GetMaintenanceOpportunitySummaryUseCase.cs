using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Scheduling;

namespace ServiceSync.Application.MaintenanceOpportunities;

public sealed class GetMaintenanceOpportunitySummaryUseCase(
    IMaintenanceOpportunityRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    ISchedulingConfiguration schedulingConfiguration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<MaintenanceOpportunitySummary>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<MaintenanceOpportunitySummary>.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulingConfiguration.DefaultTimeZoneId);
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
        return OperationResult<MaintenanceOpportunitySummary>.Success(
            await repository.GetSummaryAsync(companyId.Value, today, cancellationToken));
    }
}
