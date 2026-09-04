namespace VoltaMarcada.Application.Dashboard;

public interface IOwnerDashboardRepository
{
    Task<OwnerDashboardDto> GetAsync(
        Guid companyId,
        OwnerDashboardPeriod period,
        CancellationToken cancellationToken = default);
}
