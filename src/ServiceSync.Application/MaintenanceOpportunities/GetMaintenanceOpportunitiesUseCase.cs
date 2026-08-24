using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Scheduling;

namespace ServiceSync.Application.MaintenanceOpportunities;

public sealed class GetMaintenanceOpportunitiesUseCase(
    IMaintenanceOpportunityRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    ISchedulingConfiguration schedulingConfiguration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<IReadOnlyList<MaintenanceOpportunityListItem>>> ExecuteAsync(
        GetMaintenanceOpportunitiesQuery query,
        CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<IReadOnlyList<MaintenanceOpportunityListItem>>.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var items = await repository.GetListAsync(companyId.Value, query, GetBusinessDate(), cancellationToken);
        return OperationResult<IReadOnlyList<MaintenanceOpportunityListItem>>.Success(items);
    }

    private DateOnly GetBusinessDate()
    {
        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulingConfiguration.DefaultTimeZoneId);
        return DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone).DateTime);
    }
}
