using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Scheduling;

namespace VoltaMarcada.Application.Dashboard;

public sealed class GetOwnerDashboardUseCase(
    IOwnerDashboardRepository repository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    ISchedulingConfiguration schedulingConfiguration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<OwnerDashboardDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await authorization.GetOwnerCompanyIdAsync(cancellationToken);
        if (companyId is null)
        {
            return OperationResult<OwnerDashboardDto>.Failure(MaintenanceOpportunityErrors.Forbidden);
        }

        var timeZone = TimeZoneInfo.FindSystemTimeZoneById(schedulingConfiguration.DefaultTimeZoneId);
        var localNow = TimeZoneInfo.ConvertTime(timeProvider.GetUtcNow(), timeZone);
        var businessDate = DateOnly.FromDateTime(localNow.DateTime);
        var monthStart = new DateOnly(businessDate.Year, businessDate.Month, 1);
        var period = new OwnerDashboardPeriod(
            ToUtc(businessDate, timeZone),
            ToUtc(businessDate.AddDays(1), timeZone),
            ToUtc(monthStart, timeZone),
            ToUtc(monthStart.AddMonths(1), timeZone),
            businessDate);

        return OperationResult<OwnerDashboardDto>.Success(
            await repository.GetAsync(companyId.Value, period, cancellationToken));
    }

    private static DateTimeOffset ToUtc(DateOnly date, TimeZoneInfo timeZone)
    {
        var localMidnight = date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified);
        return new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localMidnight, timeZone), TimeSpan.Zero);
    }
}
