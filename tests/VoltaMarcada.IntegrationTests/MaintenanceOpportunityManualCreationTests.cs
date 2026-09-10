using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Application.EmployeeInvitations;
using VoltaMarcada.Application.Equipment;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Professionals;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.Services;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class MaintenanceOpportunityManualCreationTests
{
    [Fact]
    public async Task ExecuteAsync_creates_manual_opportunity_for_current_company()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(90, null, fixture.ServiceId, "Annual maintenance")));

        Assert.True(result.IsSuccess);
        Assert.Null(Assert.Single(fixture.Repository.Added).PreviousWorkOrderId);
    }

    [Fact]
    public async Task ExecuteAsync_uses_configured_time_zone_for_interval_due_date()
    {
        var fixture = new Fixture(now: new DateTimeOffset(2026, 9, 2, 2, 30, 0, TimeSpan.Zero));

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(90, null, fixture.ServiceId, "Annual maintenance")));

        Assert.True(result.IsSuccess);
        Assert.Equal(new DateOnly(2026, 11, 30), Assert.Single(fixture.Repository.Added).DueDate);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_equipment_owned_by_another_customer()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.OtherCustomerEquipmentId,
                new(null, new DateOnly(2026, 12, 1), fixture.ServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_for_non_owner_returns_forbidden()
    {
        var fixture = new Fixture(isOwner: false);

        var result = await fixture.UseCase.ExecuteAsync(fixture.ValidCommand);

        Assert.Equal(MaintenanceOpportunityErrors.Forbidden, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_customer_from_another_company()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.OtherCompanyCustomerId, fixture.EquipmentId, fixture.ValidCommand.Recommendation));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_equipment_from_another_company()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.OtherCompanyEquipmentId, fixture.ValidCommand.Recommendation));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_service_from_another_company()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(90, null, fixture.OtherCompanyServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_inactive_service()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(90, null, fixture.InactiveServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public async Task ExecuteAsync_rejects_blank_notes(string? notes)
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(90, null, fixture.ServiceId, notes)));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_when_interval_and_due_date_are_both_missing()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(null, null, fixture.ServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_when_interval_and_due_date_are_both_provided()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(90, new DateOnly(2026, 12, 1), fixture.ServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task ExecuteAsync_rejects_non_positive_interval(int intervalDays)
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(intervalDays, null, fixture.ServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_interval_that_exceeds_date_range()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(int.MaxValue, null, fixture.ServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    [Fact]
    public async Task ExecuteAsync_rejects_minimum_due_date()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(
            new(fixture.CustomerId, fixture.EquipmentId,
                new(null, DateOnly.MinValue, fixture.ServiceId, "Annual maintenance")));

        Assert.Equal(MaintenanceOpportunityErrors.InvalidInput, result.Error);
        Assert.Empty(fixture.Repository.Added);
    }

    private sealed class Fixture
    {
        private const string TimeZoneId = "America/Sao_Paulo";
        private readonly DateTimeOffset now;
        private readonly Customer customer;
        private readonly Equipment equipment;
        private readonly Service service;

        public Fixture(bool isOwner = true, DateTimeOffset? now = null)
        {
            this.now = now ?? new DateTimeOffset(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
            CompanyId = Guid.NewGuid();
            var otherCompanyId = Guid.NewGuid();
            customer = CreateCustomer(CompanyId, "12345678909");
            var otherCustomer = CreateCustomer(CompanyId, "11144477735");
            var otherCompanyCustomer = CreateCustomer(otherCompanyId, "52998224725");
            equipment = CreateEquipment(CompanyId, customer.Id);
            var otherCustomerEquipment = CreateEquipment(CompanyId, otherCustomer.Id);
            var otherCompanyEquipment = CreateEquipment(otherCompanyId, otherCompanyCustomer.Id);
            service = CreateService(CompanyId, "Maintenance");
            var inactiveService = CreateService(CompanyId, "Inactive maintenance");
            inactiveService.Deactivate(this.now);
            var otherCompanyService = CreateService(otherCompanyId, "Other maintenance");

            CustomerId = customer.Id;
            EquipmentId = equipment.Id;
            ServiceId = service.Id;
            OtherCustomerEquipmentId = otherCustomerEquipment.Id;
            OtherCompanyCustomerId = otherCompanyCustomer.Id;
            OtherCompanyEquipmentId = otherCompanyEquipment.Id;
            OtherCompanyServiceId = otherCompanyService.Id;
            InactiveServiceId = inactiveService.Id;
            Repository = new MaintenanceOpportunityRepositoryStub();

            var authorization = new MaintenanceOpportunityOwnerAuthorization(new WorkOrderAuthorization(
                new CurrentCompanyStub(CompanyId),
                new CurrentUserStub(),
                new OwnerInvitationRepositoryStub(isOwner),
                new ProfessionalRepositoryStub()));
            UseCase = new CreateManualMaintenanceOpportunityUseCase(
                Repository,
                authorization,
                new CustomerRepositoryStub([customer, otherCustomer, otherCompanyCustomer]),
                new EquipmentRepositoryStub([equipment, otherCustomerEquipment, otherCompanyEquipment]),
                new ServiceRepositoryStub([service, inactiveService, otherCompanyService]),
                new SchedulingConfigurationStub(),
                new FixedTimeProvider(this.now));
        }

        public Guid CompanyId { get; }
        public Guid CustomerId { get; }
        public Guid EquipmentId { get; }
        public Guid ServiceId { get; }
        public Guid OtherCustomerEquipmentId { get; }
        public Guid OtherCompanyCustomerId { get; }
        public Guid OtherCompanyEquipmentId { get; }
        public Guid OtherCompanyServiceId { get; }
        public Guid InactiveServiceId { get; }
        public CreateManualMaintenanceOpportunityUseCase UseCase { get; }
        public MaintenanceOpportunityRepositoryStub Repository { get; }
        public CreateManualMaintenanceOpportunityCommand ValidCommand => new(CustomerId, EquipmentId, new(90, null, ServiceId, "Annual maintenance"));

        private Customer CreateCustomer(Guid companyId, string document) => Customer.Create(
            companyId, "Customer", document, "11999999999", null, null,
            [CustomerAddress.Create(CustomerAddressType.Residence, true, "Test Street", "1", "01001000", null, Guid.NewGuid(), Guid.NewGuid())], now);

        private Equipment CreateEquipment(Guid companyId, Guid customerId) => Equipment.Create(
            companyId, customerId, "Air conditioner", null, null, null, null, null, null, null, now);

        private Service CreateService(Guid companyId, string name) => Service.Create(
            companyId, name, null, 60, ServicePriceType.Hidden, null, false, false, false, [], now);
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

    private sealed class OwnerInvitationRepositoryStub(bool isOwner) : IEmployeeInvitationRepository
    {
        public Task<bool> IsCurrentUserOwnerAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(isOwner);
        public Task<CompanyOverviewDto?> GetCompanyOverviewAsync(Guid companyId, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPendingEmailAsync(Guid companyId, string email, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPendingOrAcceptedDocumentAsync(Guid companyId, string document, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EmployeeInvitation?> GetByIdAsync(Guid invitationId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid?> GetUserIdByMembershipIdAsync(Guid membershipId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EmployeeInvitation?> GetPendingByTokenHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(EmployeeInvitation invitation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ProfessionalRepositoryStub : IProfessionalRepository
    {
        public Task AddAsync(Professional value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProfessionalListItem>> GetAllForCompanyAsync(Guid companyId, string? searchFilter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Professional?> GetForCompanyAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ProfessionalListItem?> GetForCurrentUserAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<ProfessionalListItem?>(null);
        public Task<IReadOnlyList<CompanyMemberOption>> GetAvailableCompanyMembersAsync(Guid companyId, Guid? professionalId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProfessionalServiceOption>> GetActiveServicesAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsCompanyMemberAvailableAsync(Guid companyUserId, Guid companyId, Guid? excludedProfessionalId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AreActiveServicesValidAsync(Guid companyId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CustomerRepositoryStub(IReadOnlyList<Customer> customers) : ICustomerRepository
    {
        public Task<bool> AreLocationsValidAsync(IReadOnlyCollection<CustomerAddressInput> addresses, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Customer customer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DocumentExistsAsync(Guid companyId, string normalizedDocument, Guid? excludedCustomerId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(Guid companyId, GetCustomersQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Customer?> GetForCompanyAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(customers.SingleOrDefault(customer => customer.Id == customerId && customer.CompanyId == companyId));
        public Task<Customer?> GetByDocumentAsync(Guid companyId, string normalizedDocument, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void RemoveAddresses(Customer customer) => throw new NotSupportedException();
        public void RemoveAddress(CustomerAddress address) => throw new NotSupportedException();
        public void Remove(Customer customer) => throw new NotSupportedException();
        public Task<CustomerPersistenceOutcome> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(CustomerPersistenceOutcome.Success);
    }

    private sealed class EquipmentRepositoryStub(IReadOnlyList<Equipment> equipment) : IEquipmentRepository
    {
        public Task AddAsync(Equipment value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EquipmentListItem>> GetAllForCompanyAsync(Guid companyId, GetEquipmentQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EquipmentListItem>> GetAllForCustomerAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Equipment?> GetForCompanyAsync(Guid equipmentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(equipment.SingleOrDefault(item => item.Id == equipmentId && item.CompanyId == companyId));
        public Task<EquipmentHistoryPageProjection?> GetHistoryPageForCompanyAsync(Guid equipmentId, Guid companyId, int offset, int pageSize, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Equipment?> GetActiveForCustomerByTypeAndManufacturerAsync(Guid companyId, Guid customerId, string type, string? manufacturer, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> ExistsForCustomerAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(Equipment equipment) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ServiceRepositoryStub(IReadOnlyList<Service> services) : IServiceRepository
    {
        public Task<IReadOnlyList<Service>> GetPublicBookingServicesAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Service service, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Service>> GetAllForCompanyAsync(Guid companyId, GetServicesQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Service?> GetForCompanyAsync(Guid serviceId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(services.SingleOrDefault(service => service.Id == serviceId && service.CompanyId == companyId));
        public void Remove(Service service) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class MaintenanceOpportunityRepositoryStub : IMaintenanceOpportunityRepository
    {
        public List<MaintenanceOpportunity> Added { get; } = [];

        public Task AddAsync(MaintenanceOpportunity opportunity, CancellationToken cancellationToken = default)
        {
            Added.Add(opportunity);
            return Task.CompletedTask;
        }

        public Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceBookingContext?> GetBookingContextAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceOpportunityContactContext?> GetContactContextAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(Guid companyId, GetMaintenanceOpportunitiesQuery query, DateOnly today, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class SchedulingConfigurationStub : ISchedulingConfiguration
    {
        public int SlotIntervalMinutes => 30;
        public int AvailabilitySearchDays => 7;
        public int AvailabilityResultLimit => 10;
        public string DefaultTimeZoneId => "America/Sao_Paulo";
        public IReadOnlySet<string> AllowedTimeZoneIds { get; } = new HashSet<string>(StringComparer.Ordinal) { "America/Sao_Paulo" };
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
