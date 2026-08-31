using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.Services;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class GetPublicAvailableSlotsUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_WhenRequestedDateIsToday_ShouldReturnOnlyFutureStartTimes()
    {
        var now = new DateTimeOffset(2026, 8, 31, 16, 33, 0, TimeSpan.Zero);
        var company = CreateCompany(now);
        var service = Service.Create(company.Id, "Air conditioner maintenance", null, 60, ServicePriceType.Hidden, null, true, false, false, [], now);
        var useCase = new GetPublicAvailableSlotsUseCase(
            new CompanyRepositoryStub(company),
            new ServiceRepositoryStub(service),
            new MaintenanceOpportunityRepositoryStub(),
            new FixedTimeProvider(now));

        var slots = await useCase.ExecuteAsync(company.Slug, service.Id, new DateOnly(2026, 8, 31));

        Assert.Equal(
            [new TimeOnly(14, 0), new TimeOnly(15, 0), new TimeOnly(16, 0), new TimeOnly(17, 0)],
            slots.Select(slot => slot.StartTime));
    }

    private static Company CreateCompany(DateTimeOffset now) => Company.Create(
        "Volta Marcada Ltda", "Volta Marcada", "11222333000181", "11999999999", "01001000", "Praça da Sé, 1",
        Guid.NewGuid(), Guid.NewGuid(), null, "volta-marcada", now);

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;

        public override TimeZoneInfo LocalTimeZone => TimeZoneInfo.FindSystemTimeZoneById("America/Sao_Paulo");
    }

    private sealed class CompanyRepositoryStub(Company company) : ICompanyRepository
    {
        public Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) => Task.FromResult<Company?>(id == company.Id ? company : null);
        public Task<Company?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default) => Task.FromResult<Company?>(slug == company.Slug ? company : null);
        public Task AddAsync(Company value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ServiceRepositoryStub(Service service) : IServiceRepository
    {
        public Task<IReadOnlyList<Service>> GetPublicBookingServicesAsync(Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Service>>([service]);
        public Task AddAsync(Service value, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IReadOnlyList<Service>> GetAllForCompanyAsync(Guid companyId, GetServicesQuery query, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<Service>>([service]);
        public Task<Service?> GetForCompanyAsync(Guid serviceId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Service?>(service.Id == serviceId && service.CompanyId == companyId ? service : null);
        public void Remove(Service value) { }
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MaintenanceOpportunityRepositoryStub : IMaintenanceOpportunityRepository
    {
        public Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<MaintenanceOpportunity?>(null);
        public Task<MaintenanceBookingContext?> GetBookingContextAsync(string tokenHash, CancellationToken cancellationToken = default) => Task.FromResult<MaintenanceBookingContext?>(null);
        public Task<MaintenanceOpportunityContactContext?> GetContactContextAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<MaintenanceOpportunityContactContext?>(null);
        public Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(Guid companyId, GetMaintenanceOpportunitiesQuery query, DateOnly today, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<MaintenanceOpportunityListItem>>([]);
        public Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }
}
