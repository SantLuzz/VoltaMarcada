using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class PublicBookingEquipmentTests
{
    [Fact]
    public async Task GetActiveForCustomerByTypeAndManufacturerAsync_ShouldReuseAnEquivalentActiveEquipment()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var companyId = Guid.NewGuid();
        var customerId = Guid.NewGuid();
        var equipment = Equipment.Create(companyId, customerId, "Air conditioner", "LG", null, null, null, null, null, null, DateTimeOffset.UtcNow);

        await using (var writeContext = new AppDbContext(options))
        {
            await writeContext.Equipment.AddAsync(equipment);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new AppDbContext(options);
        var repository = new EquipmentRepository(readContext);

        var resolved = await repository.GetActiveForCustomerByTypeAndManufacturerAsync(companyId, customerId, " air conditioner ", "lg");

        Assert.NotNull(resolved);
        Assert.Equal(equipment.Id, resolved!.Id);
    }
}
