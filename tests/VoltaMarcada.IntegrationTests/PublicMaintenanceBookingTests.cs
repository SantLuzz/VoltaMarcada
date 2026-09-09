using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class PublicMaintenanceBookingTests
{
    [Fact]
    public async Task GenerateMaintenanceBookingLink_supports_manual_opportunity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var useCase = new GenerateMaintenanceBookingLinkUseCase(
            fixture.Authorization,
            fixture.Opportunities,
            new AppointmentRequestRepository(fixture.DbContext),
            new PublicBookingConfigurationStub(),
            fixture.TimeProvider);

        var result = await useCase.ExecuteAsync(fixture.Opportunity.Id);

        Assert.True(result.IsSuccess);
        Assert.False(string.IsNullOrWhiteSpace(result.Value));
        Assert.Equal(MaintenanceOpportunityStatus.AwaitingCustomer, fixture.Opportunity.Status);
        Assert.Equal(PublicTokenGenerator.Hash(result.Value!), fixture.Opportunity.BookingTokenHash);
        Assert.Null(fixture.Opportunity.PreviousWorkOrderId);
    }

    [Fact]
    public async Task PrepareMaintenanceOpportunityWhatsAppContact_supports_manual_opportunity()
    {
        await using var fixture = await Fixture.CreateAsync();
        var useCase = new PrepareMaintenanceOpportunityWhatsAppContactUseCase(
            fixture.Opportunities,
            new CustomerContactRepository(fixture.DbContext),
            new WhatsAppLinkBuilderStub(),
            fixture.Authorization,
            new ApplicationUrlBuilderStub(),
            new PublicBookingConfigurationStub(),
            fixture.TimeProvider);

        var result = await useCase.ExecuteAsync(fixture.Opportunity.Id);

        Assert.True(result.IsSuccess);
        Assert.Equal(MaintenanceOpportunityStatus.AwaitingCustomer, fixture.Opportunity.Status);
        var contact = Assert.Single(fixture.DbContext.CustomerContacts);
        Assert.Equal(fixture.Opportunity.Id, contact.OpportunityId);
        Assert.Equal(fixture.Customer.Id, contact.CustomerId);
        Assert.Null(fixture.Opportunity.PreviousWorkOrderId);
    }

    private sealed class Fixture : IAsyncDisposable
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

        private Fixture(AppDbContext dbContext, Guid companyId, CurrentUserStub currentUser, Customer customer, MaintenanceOpportunity opportunity)
        {
            DbContext = dbContext;
            Customer = customer;
            Opportunity = opportunity;
            TimeProvider = new FixedTimeProvider(Now);
            Opportunities = new MaintenanceOpportunityRepository(dbContext);
            Authorization = new MaintenanceOpportunityOwnerAuthorization(new WorkOrderAuthorization(
                new CurrentCompanyStub(companyId),
                currentUser,
                new EmployeeInvitationRepository(dbContext),
                new ProfessionalRepository(dbContext)));
        }

        public AppDbContext DbContext { get; }
        public Customer Customer { get; }
        public MaintenanceOpportunity Opportunity { get; }
        public TimeProvider TimeProvider { get; }
        public MaintenanceOpportunityRepository Opportunities { get; }
        public MaintenanceOpportunityOwnerAuthorization Authorization { get; }

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
            return new Fixture(dbContext, companyId, currentUser, customer, opportunity);
        }

        public ValueTask DisposeAsync() => DbContext.DisposeAsync();
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

    private sealed class PublicBookingConfigurationStub : IPublicBookingConfiguration
    {
        public int MaintenanceLinkLifetimeDays => 7;
    }

    private sealed class ApplicationUrlBuilderStub : IApplicationUrlBuilder
    {
        public string LandingPageUrl => "https://voltamarcada.test";
        public string BuildApplicationUrl(string relativePath) => $"https://voltamarcada.test/{relativePath}";
    }

    private sealed class WhatsAppLinkBuilderStub : IWhatsAppLinkBuilder
    {
        public bool TryBuild(string phoneNumber, string message, out string link)
        {
            link = $"https://wa.me/{phoneNumber}?text={Uri.EscapeDataString(message)}";
            return true;
        }
    }
}