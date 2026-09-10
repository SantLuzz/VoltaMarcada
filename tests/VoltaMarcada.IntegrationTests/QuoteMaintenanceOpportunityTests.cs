using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Quotes;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class QuoteMaintenanceOpportunityTests
{
    [Fact]
    public async Task CreateQuote_converts_manual_opportunity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var useCase = new CreateQuoteUseCase(
            fixture.Quotes,
            new CustomerRepository(fixture.DbContext),
            new EquipmentRepository(fixture.DbContext),
            new ServiceRepository(fixture.DbContext),
            new AppointmentRepository(fixture.DbContext),
            new MaintenanceOpportunityRepository(fixture.DbContext),
            fixture.WorkOrderAuthorization,
            fixture.CurrentUser,
            fixture.TimeProvider);

        var result = await useCase.ExecuteAsync(new CreateQuoteCommand(
            Guid.Empty,
            Guid.Empty,
            Guid.Empty,
            [new QuoteItemInput("Filter replacement", 1, 150, QuoteItemType.Service)],
            0,
            null,
            new DateOnly(2026, 10, 2),
            MaintenanceOpportunityId: fixture.Opportunity.Id));

        Assert.True(result.IsSuccess);
        Assert.Equal(MaintenanceOpportunityStatus.Converted, fixture.Opportunity.Status);
        Assert.Equal(MaintenanceOpportunityConversionType.Quote, fixture.Opportunity.ConversionType);
        Assert.Equal(result.Value, fixture.Opportunity.ConvertedEntityId);
        Assert.Equal(result.Value, Assert.Single(fixture.Quotes.Added).Id);
        Assert.Null(fixture.Opportunity.PreviousWorkOrderId);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

        private Fixture(AppDbContext dbContext, CurrentUserStub currentUser, WorkOrderAuthorization workOrderAuthorization, MaintenanceOpportunity opportunity, QuoteRepositoryStub quotes)
        {
            DbContext = dbContext;
            CurrentUser = currentUser;
            WorkOrderAuthorization = workOrderAuthorization;
            Opportunity = opportunity;
            Quotes = quotes;
            TimeProvider = new FixedTimeProvider(Now);
        }

        public AppDbContext DbContext { get; }
        public CurrentUserStub CurrentUser { get; }
        public WorkOrderAuthorization WorkOrderAuthorization { get; }
        public MaintenanceOpportunity Opportunity { get; }
        public QuoteRepositoryStub Quotes { get; }
        public TimeProvider TimeProvider { get; }

        public static async Task<Fixture> CreateAsync()
        {
            var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            var companyId = Guid.NewGuid();
            var currentUser = new CurrentUserStub();
            var customer = Customer.Create(
                companyId, "Maria Silva", "52998224725", "11999999999", null, null,
                [CustomerAddress.Create(CustomerAddressType.Residence, true, "Rua A", "10", "01001000", null, Guid.NewGuid(), Guid.NewGuid())],
                Now);
            var equipment = Equipment.Create(companyId, customer.Id, "Air conditioner", "Carrier", "XPower", null, null, null, null, null, Now);
            var service = Service.Create(companyId, "Filter replacement", null, 60, ServicePriceType.Hidden, null, false, false, false, [], Now);
            var opportunity = MaintenanceOpportunity.CreateManual(companyId, customer.Id, equipment.Id, service.Id, new DateOnly(2026, 10, 1), "Annual filter replacement", Now);
            dbContext.AddRange(customer, equipment, service, opportunity, CompanyUser.Create(currentUser.UserId, companyId, CompanyUserRole.Owner, Now));
            await dbContext.SaveChangesAsync();
            var authorization = new WorkOrderAuthorization(
                new CurrentCompanyStub(companyId),
                currentUser,
                new EmployeeInvitationRepository(dbContext),
                new ProfessionalRepository(dbContext));
            return new Fixture(dbContext, currentUser, authorization, opportunity, new QuoteRepositoryStub(dbContext));
        }

        public ValueTask DisposeAsync() => DbContext.DisposeAsync();
    }

    private sealed class QuoteRepositoryStub(AppDbContext dbContext) : IQuoteRepository
    {
        public List<Quote> Added { get; } = [];

        public Task<IQuoteTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult<IQuoteTransaction>(new QuoteTransactionStub());
        public Task AddAsync(Quote quote, CancellationToken cancellationToken = default) { Added.Add(quote); return Task.CompletedTask; }
        public Task<Quote?> GetForCompanyAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Quote?> GetForCompanyForUpdateAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Quote>> GetAllForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Quote?> GetForPublicTokenAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Quote?> GetForPublicTokenForUpdateAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
    }

    private sealed class QuoteTransactionStub : IQuoteTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(companyId);
        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(companyId);
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; } = Guid.NewGuid();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}