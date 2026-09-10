using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class MaintenanceOpportunityManualPersistenceTests
{
    private readonly Fixture fixture = new();

    [Fact]
    public async Task GetListAsync_returns_null_history_for_manual_opportunity()
    {
        await fixture.AddManualOpportunityAsync();

        await using var db = fixture.CreateDbContext();
        var item = Assert.Single(await new MaintenanceOpportunityRepository(db).GetListAsync(
            fixture.CompanyId,
            new(null, null),
            new DateOnly(2026, 9, 9)));

        Assert.Null(item.PreviousWorkOrderCompletedAtUtc);
    }

    private sealed class Fixture
    {
        private static readonly DateTimeOffset Now = new(2026, 9, 2, 12, 0, 0, TimeSpan.Zero);
        private readonly DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        public Guid CompanyId { get; } = Guid.NewGuid();

        public MaintenanceOpportunityRepository Repository { get; private set; } = null!;

        public AppDbContext CreateDbContext() => new(options);

        public async Task AddManualOpportunityAsync()
        {
            await using var db = CreateDbContext();
            var customer = Customer.Create(
                CompanyId,
                "Maria Silva",
                "52998224725",
                "11999999999",
                null,
                null,
                [CustomerAddress.Create(CustomerAddressType.Residence, true, "Rua A", "10", "01001000", null, Guid.NewGuid(), Guid.NewGuid())],
                Now);
            var equipment = Equipment.Create(CompanyId, customer.Id, "Air conditioner", "Carrier", "XPower", null, null, null, null, null, Now);
            var service = Service.Create(CompanyId, "Filter replacement", null, 60, ServicePriceType.Hidden, null, false, false, false, [], Now);
            var opportunity = MaintenanceOpportunity.CreateManual(
                CompanyId,
                customer.Id,
                equipment.Id,
                service.Id,
                new DateOnly(2026, 10, 1),
                "Annual filter replacement",
                Now);

            db.AddRange(customer, equipment, service, opportunity);
            await db.SaveChangesAsync();

        }
    }
}