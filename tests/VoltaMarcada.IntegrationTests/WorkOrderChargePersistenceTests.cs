using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChargePersistenceTests
{
    [Fact]
    public void WorkOrderCharge_IsMappedWithAUniqueWorkOrderConstraint()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var dbContext = new AppDbContext(options);
        var charge = dbContext.Model.FindEntityType(typeof(WorkOrderCharge));

        Assert.NotNull(charge);
        Assert.Contains(charge!.GetIndexes(), index => index.IsUnique && index.Properties.Select(property => property.Name).SequenceEqual([nameof(WorkOrderCharge.WorkOrderId)]));
    }
}
