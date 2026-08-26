using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IMaintenanceOpportunityRepository
{
    Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default);
    Task<MaintenanceBookingContext?> GetBookingContextAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunityContactContext?> GetContactContextAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(Guid companyId, GetMaintenanceOpportunitiesQuery query, DateOnly today, CancellationToken cancellationToken = default);
    Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
    Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);
}

public sealed record MaintenanceBookingContext(
    MaintenanceOpportunity Opportunity,
    string CompanySlug,
    string CompanyTradeName,
    string? CompanyLogoKey,
    string CompanyWhatsApp,
    string CustomerName,
    string EquipmentDescription,
    string ServiceName,
    int ServiceDurationMinutes,
    bool ServiceIsActive);

public interface IMaintenanceOpportunityTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}
