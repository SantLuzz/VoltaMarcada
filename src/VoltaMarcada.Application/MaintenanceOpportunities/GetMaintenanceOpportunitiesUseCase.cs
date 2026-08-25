using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

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
