using Microsoft.EntityFrameworkCore;
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
}
