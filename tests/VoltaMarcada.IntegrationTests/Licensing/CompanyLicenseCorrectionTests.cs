using System.Reflection;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Configuration;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Migrations;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using VoltaMarcada.Web.Configuration;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class CompanyLicenseCorrectionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Trial_options_bind_the_existing_snake_case_configuration_key()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["parameters:free_trial_days"] = "21"
            })
            .Build();

        var options = configuration
            .GetRequiredSection(CompanyLicenseTrialOptions.SectionName)
            .Get<CompanyLicenseTrialOptions>();

        Assert.NotNull(options);
        Assert.Equal(21, options.FreeTrialDays);
    }

    [Fact]
    public void Migration_backfills_existing_companies_with_a_fresh_deployment_trial()
    {
        var migration = new AddCompanyLicenses();
        var builder = new MigrationBuilder("Npgsql.EntityFrameworkCore.PostgreSQL");

        typeof(AddCompanyLicenses)
            .GetMethod("Up", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(migration, [builder]);

        var backfill = Assert.Single(builder.Operations.OfType<SqlOperation>());

        Assert.Contains("CURRENT_TIMESTAMP", backfill.Sql, StringComparison.Ordinal);
        Assert.Contains("CURRENT_TIMESTAMP + INTERVAL '14 days'", backfill.Sql, StringComparison.Ordinal);
        Assert.DoesNotContain("\"CreatedAtUtc\" + INTERVAL '14 days'", backfill.Sql, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateCompanyAndOwnerAsync_rolls_back_company_and_membership_when_license_insert_fails()
    {
        await using var database = await RelationalDatabase.CreateAsync();
        var company = database.CreateCompany();
        var membership = CompanyUser.Create(database.UserId, company.Id, CompanyUserRole.Owner, Now);
        var invalidLicense = CompanyLicense.Create(Guid.NewGuid(), Now, 14);

        await using (var db = database.CreateDbContext())
        {
            var repository = new CompanyOnboardingRepository(db);

            await Assert.ThrowsAsync<DbUpdateException>(() => repository.CreateCompanyAndOwnerAsync(
                company,
                membership,
                invalidLicense,
                CancellationToken.None));
        }

        await using var verification = database.CreateDbContext();
        Assert.False(await verification.Companies.AnyAsync(value => value.Id == company.Id));
        Assert.False(await verification.CompanyUsers.AnyAsync(value => value.Id == membership.Id));
    }

    [Fact]
    public async Task Company_license_relation_is_unique_in_the_relational_database()
    {
        await using var database = await RelationalDatabase.CreateAsync();
        var company = database.CreateCompany();

        await using (var db = database.CreateDbContext())
        {
            db.Companies.Add(company);
            db.CompanyLicenses.Add(CompanyLicense.Create(company.Id, Now, 14));
            await db.SaveChangesAsync();
        }

        await using var duplicateContext = database.CreateDbContext();
        duplicateContext.CompanyLicenses.Add(CompanyLicense.Create(company.Id, Now, 14));

        await Assert.ThrowsAsync<DbUpdateException>(() => duplicateContext.SaveChangesAsync());
    }

    private sealed class RelationalDatabase : IAsyncDisposable
    {
        private readonly SqliteConnection connection;
        private readonly DbContextOptions<AppDbContext> options;
        private readonly Guid stateId = Guid.NewGuid();
        private readonly Guid cityId = Guid.NewGuid();

        private RelationalDatabase(SqliteConnection connection)
        {
            this.connection = connection;
            options = new DbContextOptionsBuilder<AppDbContext>()
                .UseSqlite(connection)
                .Options;
            UserId = Guid.NewGuid();
        }

        public Guid UserId { get; }

        public static async Task<RelationalDatabase> CreateAsync()
        {
            var connection = new SqliteConnection("Data Source=:memory:;Foreign Keys=True");
            await connection.OpenAsync();

            var database = new RelationalDatabase(connection);
            await using var db = database.CreateDbContext();
            await db.Database.EnsureCreatedAsync();
            db.States.Add(State.Create(database.stateId, "Sao Paulo", "SP"));
            db.Cities.Add(City.Create(database.cityId, database.stateId, "Sao Paulo"));
            db.Users.Add(new ApplicationUser { Id = database.UserId, UserName = "owner@example.com", Email = "owner@example.com" });
            await db.SaveChangesAsync();

            return database;
        }

        public AppDbContext CreateDbContext() => new(options);

        public Company CreateCompany() => Company.Create(
            "Volta Marcada Servicos Ltda",
            "Volta Marcada",
            "11222333000181",
            "11999999999",
            "01001000",
            "Praca da Se, 1",
            stateId,
            cityId,
            null,
            Guid.NewGuid().ToString("N"),
            Now);

        public async ValueTask DisposeAsync()
        {
            await connection.DisposeAsync();
        }
    }
}
