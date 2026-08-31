using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Persistence;

public sealed class PhaseSevenMigrationTests
{
    [Fact]
    public void Migrations_ShouldDiscoverEveryPhaseSevenMigration()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql("Host=localhost;Database=volta_marcada_migration_catalog;Username=test;Password=test")
            .Options;

        using var dbContext = new AppDbContext(options);
        var migrations = dbContext.GetService<IMigrationsAssembly>().Migrations;

        Assert.Contains("20260827100000_AddCompanyBusinessHours", migrations.Keys);
        Assert.Contains("20260827103000_MakeAppointmentRequestProfessionalOptional", migrations.Keys);
        Assert.Contains("20260831120000_RepairCompanyBusinessHours", migrations.Keys);
    }

    [Fact]
    public async Task SaveCompany_ShouldPersistDefaultBusinessHoursForEveryDay()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var company = VoltaMarcada.Domain.Entities.Company.Create(
            "VoltaMarcada Test Company Ltd.",
            "VoltaMarcada Test Company",
            "04.252.011/0001-10",
            "11999999999",
            "01001-000",
            "Praca da Se, 1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "volta-marcada-test-company",
            new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero));

        await using (var writeContext = new AppDbContext(options))
        {
            writeContext.Companies.Add(company);
            await writeContext.SaveChangesAsync();
        }

        await using var readContext = new AppDbContext(options);
        var persistedCompany = await readContext.Companies
            .Include(item => item.BusinessHours)
            .SingleAsync(item => item.Id == company.Id);

        Assert.Equal(7, persistedCompany.BusinessHours.Count);
        Assert.Equal(
            Enum.GetValues<DayOfWeek>(),
            persistedCompany.BusinessHours.OrderBy(item => item.DayOfWeek).Select(item => item.DayOfWeek));
    }
}
