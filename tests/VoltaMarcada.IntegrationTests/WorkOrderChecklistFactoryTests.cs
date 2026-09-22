using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChecklistFactoryTests
{
    [Fact]
    public async Task CreateAsync_WithTheSameServiceForTwoEquipmentItems_CreatesAnIdentifiedChecklistForEachItem()
    {
        var now = new DateTimeOffset(2026, 9, 22, 12, 0, 0, TimeSpan.Zero);
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var companyId = Guid.NewGuid();
        var customer = Customer.Create(
            companyId,
            "Customer",
            "12345678909",
            "11999999999",
            null,
            null,
            [CustomerAddress.Create(CustomerAddressType.Residence, true, "Street", "1", "01001000", null, Guid.NewGuid(), Guid.NewGuid())],
            now);
        var service = Service.Create(companyId, "Cleaning", null, 60, ServicePriceType.Fixed, 150m, true, false, false,
            [ServiceChecklistItem.Create("Clean filters", true, 0)], now);
        var equipmentA = Equipment.Create(companyId, customer.Id, "Air conditioner", "LG", "Dual", null, null, null, null, null, now);
        var equipmentB = Equipment.Create(companyId, customer.Id, "Air conditioner", "LG", "Inverter", null, null, null, null, null, now);
        dbContext.AddRange(customer, service, equipmentA, equipmentB);
        await dbContext.SaveChangesAsync();

        var result = await new WorkOrderChecklistFactory(
            new ServiceRepository(dbContext),
            new EquipmentRepository(dbContext))
            .CreateAsync(
                companyId,
                [
                    new WorkOrderItemDefinition("Cleaning", 1, 150m, WorkOrderItemType.Service, equipmentA.Id, service.Id),
                    new WorkOrderItemDefinition("Cleaning", 1, 150m, WorkOrderItemType.Service, equipmentB.Id, service.Id)
                ]);

        Assert.Equal(
        [
            "Cleaning — Air conditioner LG Dual: Clean filters",
            "Cleaning — Air conditioner LG Inverter: Clean filters"
        ], result.Select(item => item.Description));
        Assert.All(result, item => Assert.True(item.IsRequired));
    }
}
