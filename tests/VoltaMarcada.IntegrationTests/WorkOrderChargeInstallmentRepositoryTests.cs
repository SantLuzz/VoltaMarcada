using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChargeInstallmentRepositoryTests
{
    [Fact]
    public async Task AddAsync_PersistsEveryChargeInstallment()
    {
        await using var dbContext = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        var charge = WorkOrderCharge.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, [new WorkOrderChargeInstallmentDefinition(PaymentMethod.Pix, 100m, null)], new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));
        var repository = new WorkOrderChargeRepository(dbContext);

        await repository.AddAsync(charge);
        await repository.SaveChangesAsync();

        Assert.Single(dbContext.WorkOrderChargeInstallments);
    }
}
