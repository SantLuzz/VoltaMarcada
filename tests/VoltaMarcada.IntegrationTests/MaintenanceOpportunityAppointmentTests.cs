using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Application.Equipment;
using VoltaMarcada.Application.EmployeeInvitations;
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

public sealed class MaintenanceOpportunityAppointmentTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task CreateAsync_WhenMaintenanceOpportunityHasNoWorkOrder_ConvertsItToServiceAppointment()
    {
        var companyId = Guid.NewGuid();
        var customer = CreateCustomer(companyId);
        var equipment = Equipment.Create(companyId, customer.Id, "Air conditioner", null, null, null, null, null, null, null, Now);
        var service = Service.Create(companyId, "Maintenance", null, 60, ServicePriceType.Fixed, 150, true, false, false, [], Now);
        var professional = Professional.Create(companyId, Guid.NewGuid(), [service.Id], "America/Sao_Paulo", Now);
        var opportunity = MaintenanceOpportunity.Create(companyId, customer.Id, equipment.Id, Guid.NewGuid(), service.Id, new DateOnly(2026, 9, 2), "Maintenance reminder", Now);
        opportunity.MarkAsAwaitingCustomer(Now);
        var appointments = new AppointmentRepositoryStub();
        var useCase = CreateUseCase(companyId, professional, customer, equipment, service, opportunity, appointments);

        var result = await useCase.ExecuteAsync(new CreateAppointmentCommand(
            professional.Id,
            new DateOnly(2026, 9, 2),
            new TimeOnly(9, 0),
            60,
            null,
            null,
            null,
            null,
            null,
            opportunity.Id,
            AppointmentType.Service));

        Assert.True(result.IsSuccess);
        Assert.NotNull(appointments.AddedAppointment);
        Assert.Equal(AppointmentType.Service, appointments.AddedAppointment!.Type);
        Assert.Equal(MaintenanceOpportunityStatus.Converted, opportunity.Status);
        Assert.Equal(MaintenanceOpportunityConversionType.Appointment, opportunity.ConversionType);
        Assert.Equal(result.Value, opportunity.ConvertedEntityId);
        Assert.Null(appointments.AddedAppointment.WorkOrderId);
    }

    [Fact]
    public async Task AcceptAsync_ShouldRejectAnInactiveSelectedProfessional()
    {
        var companyId = Guid.NewGuid();
        var customer = CreateCustomer(companyId);
        var equipment = Equipment.Create(companyId, customer.Id, "Air conditioner", null, null, null, null, null, null, null, Now);
        var service = Service.Create(companyId, "Maintenance", null, 60, ServicePriceType.Fixed, 150, true, false, false, [], Now);
        var professional = Professional.Create(companyId, Guid.NewGuid(), [service.Id], "America/Sao_Paulo", Now);
        professional.Deactivate(Now);
        var request = AppointmentRequest.CreatePublicPreference(
            companyId,
            customer.Id,
            service.Id,
            equipment.Id,
            null,
            new DateOnly(2026, 9, 9),
            new TimeOnly(9, 0),
            60,
            null,
            Now);
        var appointments = new AppointmentRepositoryStub();
        var professionals = new ProfessionalRepositoryStub(professional);
        var authorization = new WorkOrderAuthorization(
            new CurrentCompanyStub(companyId),
            new CurrentUserStub(),
            new OwnerInvitationRepositoryStub(),
            professionals);
        var availability = new AppointmentAvailability(
            new AvailabilityRepositoryStub(ProfessionalAvailability.Create(
                companyId,
                professional.Id,
                [new ProfessionalDailyScheduleDefinition(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(18, 0), null, null)],
                Now)),
            appointments);
        var opportunity = MaintenanceOpportunity.Create(companyId, customer.Id, equipment.Id, Guid.NewGuid(), service.Id, new DateOnly(2026, 9, 2), "Maintenance reminder", Now);
        var useCase = new AcceptAppointmentRequestUseCase(
            authorization,
            new AppointmentRequestRepositoryStub(request),
            professionals,
            new ServiceRepositoryStub(service),
            appointments,
            new MaintenanceOpportunityRepositoryStub(opportunity),
            new CustomerRepositoryStub(customer),
            availability,
            new FixedTimeProvider(Now));

        var result = await useCase.ExecuteAsync(request.Id, professional.Id);

        Assert.False(result.IsSuccess);
        Assert.Equal(SchedulingErrors.ProfessionalNotQualified.Code, result.Error?.Code);
        Assert.Equal(AppointmentRequestStatus.Pending, request.Status);
        Assert.Null(appointments.AddedAppointment);
    }

    private static CreateAppointmentUseCase CreateUseCase(
        Guid companyId,
        Professional professional,
        Customer customer,
        global::VoltaMarcada.Domain.Entities.Equipment equipment,
        Service service,
        MaintenanceOpportunity opportunity,
        AppointmentRepositoryStub appointments)
    {
        var professionals = new ProfessionalRepositoryStub(professional);
        var authorization = new WorkOrderAuthorization(
            new CurrentCompanyStub(companyId),
            new CurrentUserStub(),
            new OwnerInvitationRepositoryStub(),
            professionals);
        var availability = new AppointmentAvailability(
            new AvailabilityRepositoryStub(ProfessionalAvailability.Create(
                companyId,
                professional.Id,
                [new ProfessionalDailyScheduleDefinition(DayOfWeek.Wednesday, new TimeOnly(8, 0), new TimeOnly(18, 0), null, null)],
                Now)),
            appointments);

        return new CreateAppointmentUseCase(
            authorization,
            professionals,
            new WorkOrderRepositoryStub(),
            new CustomerRepositoryStub(customer),
            new EquipmentRepositoryStub(equipment),
            new ServiceRepositoryStub(service),
            appointments,
            new MaintenanceOpportunityRepositoryStub(opportunity),
            availability,
            new FixedTimeProvider(Now));
    }

    private static Customer CreateCustomer(Guid companyId) => Customer.Create(
        companyId, "Customer", "12345678909", "11999999999", null, null,
        [CustomerAddress.Create(CustomerAddressType.Residence, true, "Street", "1", "01001000", null, Guid.NewGuid(), Guid.NewGuid())], Now);

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(companyId);
        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(companyId);
    }

    private sealed class AppointmentRequestRepositoryStub(AppointmentRequest request) : IAppointmentRequestRepository
    {
        public Task AddAsync(AppointmentRequest value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AppointmentRequest?> GetForCompanyAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<AppointmentRequest?>(request.Id == requestId && request.CompanyId == companyId ? request : null);
        public Task<AppointmentRequest?> GetForAppointmentAsync(Guid appointmentId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AppointmentRequest?> GetForResponseTokenAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AppointmentRequestResponseContext?> GetResponseContextAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AppointmentRequestListItem>> GetOpenForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasOpenForMaintenanceOpportunityAsync(Guid companyId, Guid opportunityId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CurrentUserStub : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; } = Guid.NewGuid();
    }

    private sealed class OwnerInvitationRepositoryStub : IEmployeeInvitationRepository
    {
        public Task<bool> IsCurrentUserOwnerAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<CompanyOverviewDto?> GetCompanyOverviewAsync(Guid companyId, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPendingEmailAsync(Guid companyId, string email, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasPendingOrAcceptedDocumentAsync(Guid companyId, string document, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EmployeeInvitation?> GetByIdAsync(Guid invitationId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Guid?> GetUserIdByMembershipIdAsync(Guid membershipId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<EmployeeInvitation?> GetPendingByTokenHashAsync(string tokenHash, DateTimeOffset now, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(EmployeeInvitation invitation, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ProfessionalRepositoryStub(Professional professional) : IProfessionalRepository
    {
        public Task AddAsync(Professional value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProfessionalListItem>> GetAllForCompanyAsync(Guid companyId, string? searchFilter, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Professional?> GetForCompanyAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Professional?>(professional.Id == professionalId && professional.CompanyId == companyId ? professional : null);
        public Task<ProfessionalListItem?> GetForCurrentUserAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<ProfessionalListItem?>(null);
        public Task<IReadOnlyList<CompanyMemberOption>> GetAvailableCompanyMembersAsync(Guid companyId, Guid? professionalId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ProfessionalServiceOption>> GetActiveServicesAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> IsCompanyMemberAvailableAsync(Guid companyUserId, Guid companyId, Guid? excludedProfessionalId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> AreActiveServicesValidAsync(Guid companyId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class WorkOrderRepositoryStub : IWorkOrderRepository
    {
        public Task AddAsync(WorkOrder workOrder, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<WorkOrder?> GetForAccessAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) => Task.FromResult<WorkOrder?>(null);
        public Task<IReadOnlyList<WorkOrderListItem>> GetAllForAccessAsync(Guid companyId, Guid? professionalId, GetWorkOrdersQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class CustomerRepositoryStub(Customer customer) : ICustomerRepository
    {
        public Task<bool> AreLocationsValidAsync(IReadOnlyCollection<CustomerAddressInput> addresses, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Customer value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> DocumentExistsAsync(Guid companyId, string normalizedDocument, Guid? excludedCustomerId = null, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(Guid companyId, GetCustomersQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Customer?> GetForCompanyAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Customer?>(customer.Id == customerId && customer.CompanyId == companyId ? customer : null);
        public Task<Customer?> GetByDocumentAsync(Guid companyId, string normalizedDocument, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void RemoveAddresses(Customer customer) => throw new NotSupportedException();
        public void RemoveAddress(CustomerAddress address) => throw new NotSupportedException();
        public void Remove(Customer customer) => throw new NotSupportedException();
        public Task<CustomerPersistenceOutcome> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(CustomerPersistenceOutcome.Success);
    }

    private sealed class EquipmentRepositoryStub(global::VoltaMarcada.Domain.Entities.Equipment equipment) : IEquipmentRepository
    {
        public Task AddAsync(global::VoltaMarcada.Domain.Entities.Equipment value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EquipmentListItem>> GetAllForCompanyAsync(Guid companyId, GetEquipmentQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<EquipmentListItem>> GetAllForCustomerAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<global::VoltaMarcada.Domain.Entities.Equipment?> GetForCompanyAsync(Guid equipmentId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<global::VoltaMarcada.Domain.Entities.Equipment?>(equipment.Id == equipmentId && equipment.CompanyId == companyId ? equipment : null);
        public Task<global::VoltaMarcada.Domain.Entities.Equipment?> GetActiveForCustomerByTypeAndManufacturerAsync(Guid companyId, Guid customerId, string type, string? manufacturer, CancellationToken cancellationToken = default) => Task.FromResult<global::VoltaMarcada.Domain.Entities.Equipment?>(null);
        public Task<bool> ExistsForCustomerAsync(Guid customerId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void Remove(global::VoltaMarcada.Domain.Entities.Equipment equipment) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class ServiceRepositoryStub(Service service) : IServiceRepository
    {
        public Task<IReadOnlyList<Service>> GetPublicBookingServicesAsync(Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAsync(Service value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Service>> GetAllForCompanyAsync(Guid companyId, GetServicesQuery query, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Service?> GetForCompanyAsync(Guid serviceId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<Service?>(service.Id == serviceId && service.CompanyId == companyId ? service : null);
        public void Remove(Service service) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AppointmentRepositoryStub : IAppointmentRepository
    {
        public Appointment? AddedAppointment { get; private set; }
        public Task<IAppointmentSchedulingTransaction> BeginSchedulingTransactionAsync(Guid companyId, Guid professionalId, CancellationToken cancellationToken = default) => Task.FromResult<IAppointmentSchedulingTransaction>(new SchedulingTransactionStub());
        public Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default) { AddedAppointment = appointment; return Task.CompletedTask; }
        public Task AddBlockRequestAsync(ScheduleBlockRequest request, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<Appointment?> GetForAccessAsync(Guid appointmentId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<ScheduleBlockRequest?> GetBlockRequestForAccessAsync(Guid requestId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Appointment>> GetRangeForAccessAsync(Guid companyId, Guid? professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<Appointment>> GetForWorkOrderAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AppointmentListItem>> GetListRangeForAccessAsync(Guid companyId, Guid? professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<AppointmentListItem>> GetListForWorkOrderAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<ScheduleBlockRequest>> GetBlockRequestsForAccessAsync(Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<bool> HasAppointmentConflictAsync(Guid companyId, Guid professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, Guid? excludedAppointmentId, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task<bool> HasOpenAppointmentsAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<AppointmentPersistenceOutcome> TrySaveAppointmentChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(AppointmentPersistenceOutcome.Saved);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class SchedulingTransactionStub : IAppointmentSchedulingTransaction
    {
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class AvailabilityRepositoryStub(ProfessionalAvailability availability) : IProfessionalAvailabilityRepository
    {
        public Task<bool> ProfessionalExistsAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(true);
        public Task<ProfessionalAvailability?> GetAvailabilityAsync(Guid professionalId, Guid companyId, bool trackChanges, CancellationToken cancellationToken = default) => Task.FromResult<ProfessionalAvailability?>(availability);
        public Task<IReadOnlyList<ScheduleBlock>> GetScheduleBlocksAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<ScheduleBlock>>([]);
        public Task<ScheduleBlock?> GetScheduleBlockAsync(Guid scheduleBlockId, Guid professionalId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddAvailabilityAsync(ProfessionalAvailability availability, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task AddScheduleBlockAsync(ScheduleBlock scheduleBlock, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public void RemoveScheduleBlock(ScheduleBlock scheduleBlock) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    private sealed class MaintenanceOpportunityRepositoryStub(MaintenanceOpportunity opportunity) : IMaintenanceOpportunityRepository
    {
        public Task AddAsync(MaintenanceOpportunity value, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceOpportunity?> GetForCompanyAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult<MaintenanceOpportunity?>(opportunity.Id == opportunityId && opportunity.CompanyId == companyId ? opportunity : null);
        public Task<MaintenanceBookingContext?> GetBookingContextAsync(string tokenHash, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceOpportunityContactContext?> GetContactContextAsync(Guid opportunityId, Guid companyId, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<IReadOnlyList<MaintenanceOpportunityListItem>> GetListAsync(Guid companyId, GetMaintenanceOpportunitiesQuery query, DateOnly today, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task<MaintenanceOpportunitySummary> GetSummaryAsync(Guid companyId, DateOnly today, CancellationToken cancellationToken = default) => throw new NotSupportedException();
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<IMaintenanceOpportunityTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => throw new NotSupportedException();
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
