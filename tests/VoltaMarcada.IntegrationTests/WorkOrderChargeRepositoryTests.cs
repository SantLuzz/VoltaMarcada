using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChargeRepositoryTests
{
    [Fact]
    public async Task GetForWorkOrderAsync_FiltersByCompanyAndWorkOrder()
    {
        var companyId = Guid.NewGuid();
        var workOrderId = Guid.NewGuid();
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options);
        var charge = WorkOrderCharge.Create(companyId, workOrderId, 100m,
            [new WorkOrderChargeInstallmentDefinition(PaymentMethod.Cash, 100m, null)],
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));
        await dbContext.WorkOrderCharges.AddAsync(charge);
        await dbContext.SaveChangesAsync();

        var repository = new WorkOrderChargeRepository(dbContext);

        Assert.NotNull(await repository.GetForWorkOrderAsync(companyId, workOrderId));
        Assert.Null(await repository.GetForWorkOrderAsync(Guid.NewGuid(), workOrderId));
    }
}
