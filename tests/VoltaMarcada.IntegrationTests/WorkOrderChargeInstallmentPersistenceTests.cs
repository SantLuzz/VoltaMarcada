using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class WorkOrderChargeInstallmentPersistenceTests
{
    [Fact]
    public void WorkOrderChargeInstallment_IsMappedWithAChargeForeignKey()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var dbContext = new AppDbContext(options);
        var installment = dbContext.Model.FindEntityType(typeof(WorkOrderChargeInstallment));

        Assert.NotNull(installment);
        Assert.Contains(installment!.GetForeignKeys(), foreignKey => foreignKey.Properties.Select(property => property.Name).SequenceEqual([nameof(WorkOrderChargeInstallment.WorkOrderChargeId)]));
    }
}
