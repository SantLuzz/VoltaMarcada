using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderOrderingTests
{
    [Fact]
    public async Task CreateWorkOrder_WithMultipleServiceItems_PersistsEveryServiceAssociation()
    {
        await using var fixture = await WorkOrderFixture.CreateAsync();

        var result = await fixture.CreateUseCase.ExecuteAsync(fixture.ValidCommand with
        {
            Items =
            [
                new WorkOrderServiceItemInput(fixture.EquipmentA.Id, fixture.ServiceA.Id, "Maintenance", 1, 100m),
                new WorkOrderServiceItemInput(fixture.EquipmentB.Id, fixture.ServiceB.Id, "Repair", 1, 150m)
            ]
        });

        Assert.True(result.IsSuccess);
        var workOrder = await fixture.Repository.GetForAccessAsync(result.Value!.WorkOrderId!.Value, fixture.CompanyId, null);
        Assert.NotNull(workOrder);
        Assert.Equal(2, workOrder!.Items.Count(item => item.ServiceId is not null));
        Assert.Equal(fixture.EquipmentB.Id, workOrder.Items.Single(item => item.ServiceId == fixture.ServiceB.Id).EquipmentId);
    }

    [Fact]
    public async Task CreateWorkOrder_WithServiceItemEquipmentFromAnotherCustomer_RejectsTheCommand()
    {
        await using var fixture = await WorkOrderFixture.CreateAsync();
        var anotherCustomer = CreateCustomer(fixture.CompanyId);
        var equipmentForAnotherCustomer = Equipment.Create(fixture.CompanyId, anotherCustomer.Id, "Freezer", null, null, null, null, null, null, null, Now);
        fixture.DbContext.AddRange(anotherCustomer, equipmentForAnotherCustomer);
        await fixture.DbContext.SaveChangesAsync();

        var result = await fixture.CreateUseCase.ExecuteAsync(fixture.ValidCommand with
        {
            Items =
            [
                new WorkOrderServiceItemInput(fixture.EquipmentA.Id, fixture.ServiceA.Id, "Maintenance", 1, 100m),
                new WorkOrderServiceItemInput(equipmentForAnotherCustomer.Id, fixture.ServiceB.Id, "Repair", 1, 150m)
            ]
        });

        Assert.False(result.IsSuccess);
        Assert.Equal(WorkOrderErrors.InvalidEquipmentCustomer.Code, result.Error?.Code);
    }

    [Fact]
    public async Task GetAllForAccessAsync_ShouldOrderActiveWorkOrdersByScheduleAndKeepCompletedLast()
    {
        var companyId = Guid.NewGuid();
        var customer = CreateCustomer(companyId);
        var equipment = Equipment.Create(companyId, customer.Id, "Air conditioner", null, null, null, null, null, null, null, Now);
        var service = Service.Create(companyId, "Maintenance", null, 60, ServicePriceType.Fixed, 150m, true, false, false, [], Now);
        var professionalId = Guid.NewGuid();
        var completed = CreateWorkOrder(companyId, customer, equipment, service, professionalId, Now.AddDays(-3));
        completed.Start(Now.AddDays(-2));
        completed.Complete(false, false, Now.AddDays(-1));
        var laterActive = CreateWorkOrder(companyId, customer, equipment, service, professionalId, Now.AddDays(-2));
        var earlierActive = CreateWorkOrder(companyId, customer, equipment, service, professionalId, Now.AddDays(-1));

        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        dbContext.AddRange(customer, equipment, service, completed, laterActive, earlierActive);
        dbContext.Appointments.AddRange(
            CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, completed.Id, Now.AddDays(-3)),
            CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, laterActive.Id, Now.AddDays(3)),
            CreateAppointment(companyId, professionalId, customer.Id, equipment.Id, service.Id, earlierActive.Id, Now.AddDays(2)));
        await dbContext.SaveChangesAsync();

        var result = await new WorkOrderRepository(dbContext).GetAllForAccessAsync(
            companyId,
            null,
            new GetWorkOrdersQuery(null, null));

        Assert.Equal([earlierActive.Id, laterActive.Id, completed.Id], result.Select(item => item.Id));
        Assert.Equal(Now.AddDays(2), result[0].ScheduledAtUtc);
        Assert.Equal(Now.AddDays(3), result[1].ScheduledAtUtc);
    }

    private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);

    private static Customer CreateCustomer(Guid companyId) => Customer.Create(
        companyId,
        "Customer",
        "12345678909",
        "11999999999",
        null,
        null,
        [CustomerAddress.Create(CustomerAddressType.Residence, true, "Street", "1", "01001000", null, Guid.NewGuid(), Guid.NewGuid())],
        Now);

    private static WorkOrder CreateWorkOrder(
        Guid companyId,
        Customer customer,
        Equipment equipment,
        Service service,
        Guid professionalId,
        DateTimeOffset createdAtUtc) => WorkOrder.Create(
        companyId,
        customer.Id,
        equipment.Id,
        service.Id,
        professionalId,
        Guid.NewGuid(),
        WorkOrderPriority.Normal,
        null,
        [new WorkOrderItemDefinition(service.Name, 1, 150m, WorkOrderItemType.Service)],
        [],
        createdAtUtc);

    private static Appointment CreateAppointment(
        Guid companyId,
        Guid professionalId,
        Guid customerId,
        Guid equipmentId,
        Guid serviceId,
        Guid workOrderId,
        DateTimeOffset startAtUtc) => Appointment.Create(
        companyId,
        professionalId,
        customerId,
        equipmentId,
        serviceId,
        workOrderId,
        startAtUtc,
        startAtUtc.AddHours(1),
        null,
        Now,
        AppointmentOrigin.Internal,
        AppointmentType.Service);
    private sealed class WorkOrderFixture : IAsyncDisposable
    {
        private WorkOrderFixture(AppDbContext dbContext, Guid companyId, WorkOrderRepository repository, CreateWorkOrderUseCase createUseCase, Customer customer, Equipment equipmentA, Equipment equipmentB, Service serviceA, Service serviceB)
        {
            DbContext = dbContext;
            CompanyId = companyId;
            Repository = repository;
            CreateUseCase = createUseCase;
            EquipmentA = equipmentA;
            EquipmentB = equipmentB;
            ServiceA = serviceA;
            ServiceB = serviceB;
            ValidCommand = new CreateWorkOrderCommand(customer.Id, equipmentA.Id, serviceA.Id, null, WorkOrderPriority.Normal, null, null, 0, []);
        }

        public AppDbContext DbContext { get; }
        public Guid CompanyId { get; }
        public WorkOrderRepository Repository { get; }
        public CreateWorkOrderUseCase CreateUseCase { get; }
        public Equipment EquipmentA { get; }
        public Equipment EquipmentB { get; }
        public Service ServiceA { get; }
        public Service ServiceB { get; }
        public CreateWorkOrderCommand ValidCommand { get; }

        public static async Task<WorkOrderFixture> CreateAsync()
        {
            var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options);
            var companyId = Guid.NewGuid();
            var user = new CurrentUserStub();
            var customer = CreateCustomer(companyId);
            var equipmentA = Equipment.Create(companyId, customer.Id, "Air conditioner", null, null, null, null, null, null, null, Now);
            var equipmentB = Equipment.Create(companyId, customer.Id, "Freezer", null, null, null, null, null, null, null, Now);
            var serviceA = Service.Create(companyId, "Maintenance", null, 60, ServicePriceType.Fixed, 100m, true, false, false, [], Now);
            var serviceB = Service.Create(companyId, "Repair", null, 60, ServicePriceType.Fixed, 150m, true, false, false, [], Now);
            dbContext.AddRange(customer, equipmentA, equipmentB, serviceA, serviceB, CompanyUser.Create(user.UserId, companyId, CompanyUserRole.Owner, Now));
            await dbContext.SaveChangesAsync();

            var repository = new WorkOrderRepository(dbContext);
            var appointments = new AppointmentRepository(dbContext);
            var authorization = new WorkOrderAuthorization(new CurrentCompanyStub(companyId), user, new EmployeeInvitationRepository(dbContext), new ProfessionalRepository(dbContext));
            var createUseCase = new CreateWorkOrderUseCase(
                repository,
                new CustomerRepository(dbContext),
                new EquipmentRepository(dbContext),
                new ServiceRepository(dbContext),
                new ProfessionalRepository(dbContext),
                appointments,
                new AppointmentAvailability(new ProfessionalAvailabilityRepository(dbContext), appointments),
                authorization,
                user,
                new CreateWorkOrderCommandValidator(),
                new WorkOrderChecklistFactory(new ServiceRepository(dbContext), new EquipmentRepository(dbContext)),
                new FixedTimeProvider(Now));
            return new WorkOrderFixture(dbContext, companyId, repository, createUseCase, customer, equipmentA, equipmentB, serviceA, serviceB);
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

}
