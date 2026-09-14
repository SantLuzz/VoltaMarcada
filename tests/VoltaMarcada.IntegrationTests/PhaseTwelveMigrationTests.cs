using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class PhaseTwelveMigrationTests
{
    [Fact]
    public void Migrations_ShouldDiscoverWorkOrderPaymentControlMigration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=volta_marcada_migration_catalog;Username=test;Password=test")
            .Options;
        using var dbContext = new AppDbContext(options);
        var migrations = dbContext.GetService<IMigrationsAssembly>().Migrations;

        Assert.Contains(migrations.Keys, key => key.EndsWith("_AddWorkOrderPaymentControl", StringComparison.Ordinal));
    }
}
