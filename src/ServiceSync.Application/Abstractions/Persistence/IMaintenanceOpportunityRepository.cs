using ServiceSync.Application.MaintenanceOpportunities;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IMaintenanceOpportunityRepository
{
    Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(Guid companyId, GetMaintenanceOpportunitiesQuery query, DateOnly today, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public interface IMaintenanceOpportunityTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
