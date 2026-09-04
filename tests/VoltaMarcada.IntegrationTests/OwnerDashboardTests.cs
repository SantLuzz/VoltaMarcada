using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Dashboard;
using VoltaMarcada.Application.EmployeeInvitations;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Professionals;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class OwnerDashboardTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 3, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task ExecuteAsync_ForOwner_ProjectsCurrentCompanyActionsAndLocalMonthMaintenanceFunnel()
    {
        var companyId = Guid.NewGuid();
        var secondCompanyId = Guid.NewGuid();
        var customer = CreateCustomer(companyId, "Customer", Now);
        var secondCustomer = CreateCustomer(secondCompanyId, "Second customer", Now);
        var equipment = CreateEquipment(companyId, customer.Id, Now);
        var secondEquipment = CreateEquipment(secondCompanyId, secondCustomer.Id, Now);
        var service = CreateService(companyId, 100m, Now);
        var secondService = CreateService(secondCompanyId, 200m, Now);
        var professionalId = Guid.NewGuid();
        var secondProfessionalId = Guid.NewGuid();

        var overdue = CreateOpportunity(companyId, customer.Id, equipment.Id, service.Id, new DateOnly(2026, 9, 2), new(2026, 9, 1, 3, 0, 0, TimeSpan.Zero));
        var dueSoon = CreateOpportunity(companyId, customer.Id, equipment.Id, service.Id, new DateOnly(2026, 9, 3), new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero));
        var recoveredOpportunity = CreateOpportunity(companyId, customer.Id, equipment.Id, service.Id, new DateOnly(2026, 9, 5), new(2026, 9, 3, 3, 0, 0, TimeSpan.Zero));
        var quoteOnlyOpportunity = CreateOpportunity(companyId, customer.Id, equipment.Id, service.Id, new DateOnly(2026, 9, 6), new(2026, 9, 3, 12, 0, 0, TimeSpan.Zero));
        var outsideMonth = CreateOpportunity(companyId, customer.Id, equipment.Id, service.Id, new DateOnly(2026, 9, 10), new(2026, 9, 1, 2, 59, 0, TimeSpan.Zero));
        var secondCompanyOpportunity = CreateOpportunity(secondCompanyId, secondCustomer.Id, secondEquipment.Id, secondService.Id, new DateOnly(2026, 9, 2), Now);

        var recoveredAppointment = CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, Now.AddHours(2));
        var cancelledAppointment = CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, new(2026, 9, 3, 3, 15, 0, TimeSpan.Zero));
        cancelledAppointment.Cancel(Now);
        recoveredOpportunity.Convert(MaintenanceOpportunityConversionType.Appointment, recoveredAppointment.Id, Now.AddHours(1));
        quoteOnlyOpportunity.Convert(MaintenanceOpportunityConversionType.Quote, Guid.NewGuid(), Now.AddHours(1));

        var recoveredRequest = CreateRequest(companyId, customer.Id, service.Id, equipment.Id, recoveredOpportunity.Id, professionalId, Now);
        recoveredRequest.Accept(recoveredAppointment.Id, Now);
        var pendingMaintenanceRequest = CreateRequest(companyId, customer.Id, service.Id, equipment.Id, dueSoon.Id, professionalId, Now);
        var unrelatedRequest = CreateRequest(companyId, customer.Id, service.Id, equipment.Id, null, professionalId, Now);
        var secondCompanyRequest = CreateRequest(secondCompanyId, secondCustomer.Id, secondService.Id, secondEquipment.Id, secondCompanyOpportunity.Id, secondProfessionalId, Now);

        var eligibleCompleted = CreateWorkOrder(companyId, customer, equipment, service, professionalId, recoveredAppointment.Id, 150m, Now.AddHours(-2));
        eligibleCompleted.Start(Now.AddHours(-1));
        eligibleCompleted.Complete(false, false, Now.AddMinutes(-30));
        var unrelatedCompleted = CreateWorkOrder(companyId, customer, equipment, service, professionalId, Guid.NewGuid(), 175m, Now.AddHours(-2));
        unrelatedCompleted.Start(Now.AddHours(-1));
        unrelatedCompleted.Complete(false, false, Now.AddMinutes(-30));
        var openPending = CreateWorkOrder(companyId, customer, equipment, service, professionalId, null, 100m, Now);
        var openInProgress = CreateWorkOrder(companyId, customer, equipment, service, professionalId, null, 100m, Now);
        openInProgress.Start(Now);
        var secondCompanyOpen = CreateWorkOrder(secondCompanyId, secondCustomer, secondEquipment, secondService, secondProfessionalId, null, 200m, Now);

        var sentQuote = CreateQuote(companyId, customer.Id, equipment.Id, service.Id, Now);
        sentQuote.Send("sent-quote", Now.AddDays(1), Now);
        var draftQuote = CreateQuote(companyId, customer.Id, equipment.Id, service.Id, Now);
        var secondCompanyQuote = CreateQuote(secondCompanyId, secondCustomer.Id, secondEquipment.Id, secondService.Id, Now);
        secondCompanyQuote.Send("second-company-quote", Now.AddDays(1), Now);

        var firstSentContact = CustomerContact.Prepare(companyId, customer.Id, overdue.Id, CustomerContactChannel.WhatsAppLink, "First contact", Now);
        firstSentContact.MarkAsSent(Now);
        var secondSentContact = CustomerContact.Prepare(companyId, customer.Id, dueSoon.Id, CustomerContactChannel.WhatsAppLink, "Second contact", Now);
        secondSentContact.MarkAsSent(Now);
        var preparedContact = CustomerContact.Prepare(companyId, customer.Id, outsideMonth.Id, CustomerContactChannel.WhatsAppLink, "Prepared contact", Now);
        var secondCompanyContact = CustomerContact.Prepare(secondCompanyId, secondCustomer.Id, secondCompanyOpportunity.Id, CustomerContactChannel.WhatsAppLink, "Second company contact", Now);
        secondCompanyContact.MarkAsSent(Now);

        await using var dbContext = CreateDbContext();
        dbContext.AddRange(
            customer, secondCustomer, equipment, secondEquipment, service, secondService,
            overdue, dueSoon, recoveredOpportunity, quoteOnlyOpportunity, outsideMonth, secondCompanyOpportunity,
            recoveredAppointment, cancelledAppointment, recoveredRequest, pendingMaintenanceRequest, unrelatedRequest, secondCompanyRequest,
            eligibleCompleted, unrelatedCompleted, openPending, openInProgress, secondCompanyOpen,
            sentQuote, draftQuote, secondCompanyQuote,
            firstSentContact, secondSentContact, preparedContact, secondCompanyContact,
            CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, new(2026, 9, 3, 3, 0, 0, TimeSpan.Zero)),
            CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, new(2026, 9, 3, 2, 59, 0, TimeSpan.Zero)),
            CreateAppointment(secondCompanyId, secondProfessionalId, secondCustomer.Id, secondEquipment.Id, secondService.Id, new(2026, 9, 3, 3, 0, 0, TimeSpan.Zero)));
        await dbContext.SaveChangesAsync();

        var result = await CreateUseCase(companyId, true, dbContext).ExecuteAsync();

        Assert.True(result.IsSuccess);
        var dashboard = Assert.IsType<OwnerDashboardDto>(result.Value);
        Assert.Equal(2, dashboard.Actions.TodayAppointmentCount);
        Assert.Equal(1, dashboard.Actions.OverdueOpportunityCount);
        Assert.Equal(2, dashboard.Actions.DueSoonOpportunityCount);
        Assert.Equal(2, dashboard.Actions.PendingAppointmentRequestCount);
        Assert.Equal(1, dashboard.Actions.QuotesAwaitingResponseCount);
        Assert.Equal(2, dashboard.Actions.OpenWorkOrderCount);
        Assert.Equal(4, dashboard.ValueFunnel.IdentifiedOpportunityCount);
        Assert.Equal(1, dashboard.ValueFunnel.ContactedCustomerCount);
        Assert.Equal(2, dashboard.ValueFunnel.MaintenanceRequestCount);
        Assert.Equal(1, dashboard.ValueFunnel.RecoveredBookingCount);
        Assert.Equal(1, dashboard.ValueFunnel.CompletedServiceCount);
        Assert.Equal(150m, dashboard.ValueFunnel.RecoveredRevenue);
        Assert.Equal(3, dashboard.ValueFunnel.OpenOpportunityCount);
        Assert.Equal(300m, dashboard.ValueFunnel.EstimatedPotentialRevenue);
    }

    [Fact]
    public async Task ExecuteAsync_ForEmployee_ReturnsForbidden()
    {
        await using var dbContext = CreateDbContext();

        var result = await CreateUseCase(Guid.NewGuid(), false, dbContext).ExecuteAsync();

        Assert.False(result.IsSuccess);
        Assert.Equal(MaintenanceOpportunityErrors.Forbidden, result.Error);
    }

    private static GetOwnerDashboardUseCase CreateUseCase(Guid companyId, bool isOwner, AppDbContext dbContext) => new(
        new OwnerDashboardRepository(dbContext),
        new MaintenanceOpportunityOwnerAuthorization(new WorkOrderAuthorization(
            new CurrentCompanyStub(companyId),
            new CurrentUserStub(),
            new OwnerInvitationRepositoryStub(isOwner),
            new ProfessionalRepositoryStub())),
        new SchedulingConfigurationStub(),
        new FixedTimeProvider(Now));

    private static AppDbContext CreateDbContext() => new(new DbContextOptionsBuilder<AppDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString())
        .Options);

    private static Customer CreateCustomer(Guid companyId, string name, DateTimeOffset now) => Customer.Create(
        companyId, name, "12345678909", "11999999999", null, null,
        [CustomerAddress.Create(CustomerAddressType.Residence, true, "Test Street", "1", "01001000", null, Guid.NewGuid(), Guid.NewGuid())], now);

    private static Equipment CreateEquipment(Guid companyId, Guid customerId, DateTimeOffset now) => Equipment.Create(
        companyId, customerId, "Air conditioner", null, null, null, null, null, null, null, now);

    private static Service CreateService(Guid companyId, decimal? basePrice, DateTimeOffset now) => Service.Create(
        companyId, "Maintenance", null, 60, ServicePriceType.Fixed, basePrice, true, false, false, [], now);

    private static MaintenanceOpportunity CreateOpportunity(Guid companyId, Guid customerId, Guid equipmentId, Guid serviceId, DateOnly dueDate, DateTimeOffset createdAtUtc) =>
        MaintenanceOpportunity.Create(companyId, customerId, equipmentId, Guid.NewGuid(), serviceId, dueDate, "Maintenance reminder", createdAtUtc);

    private static AppointmentRequest CreateRequest(Guid companyId, Guid customerId, Guid serviceId, Guid equipmentId, Guid? opportunityId, Guid professionalId, DateTimeOffset now) =>
        AppointmentRequest.Create(companyId, customerId, serviceId, professionalId, equipmentId, opportunityId, new DateOnly(2026, 9, 3), new TimeOnly(9, 0), 60, now, now.AddHours(1), null, now);

    private static Appointment CreateAppointment(Guid companyId, Guid professionalId, Guid customerId, Guid equipmentId, Guid serviceId, DateTimeOffset startAtUtc) =>
        Appointment.Create(companyId, professionalId, customerId, equipmentId, serviceId, null, startAtUtc, startAtUtc.AddHours(1), null, Now, AppointmentOrigin.Internal, AppointmentType.Service);

    private static WorkOrder CreateWorkOrder(Guid companyId, Customer customer, Equipment equipment, Service service, Guid professionalId, Guid? sourceAppointmentId, decimal totalAmount, DateTimeOffset now) =>
        WorkOrder.Create(companyId, customer.Id, equipment.Id, service.Id, professionalId, Guid.NewGuid(), WorkOrderPriority.Normal, null,
            [new WorkOrderItemDefinition(service.Name, 1, totalAmount, WorkOrderItemType.Service)], [], now, null, 0, sourceAppointmentId);

    private static Quote CreateQuote(Guid companyId, Guid customerId, Guid equipmentId, Guid serviceId, DateTimeOffset now) => Quote.Create(
        companyId, customerId, Guid.NewGuid(), equipmentId, serviceId, null, null,
        [new QuoteItemDefinition("Maintenance", 1, 100m, QuoteItemType.Service)], 0, null, new DateOnly(2026, 9, 30), now);

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
