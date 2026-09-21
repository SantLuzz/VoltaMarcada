using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using VoltaMarcada.Application.Support;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Communication;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Support;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class SupportInfrastructureTests
{
    [Fact]
    public async Task GetAsync_ReturnsContextOnlyWhenUserBelongsToCompany()
    {
        await using var database = CreateDatabase();
        var userId = Guid.NewGuid();
        var company = CreateCompany();
        database.Users.Add(new ApplicationUser
        {
            Id = userId,
            FullName = "Support member",
            Email = "member@example.com",
            UserName = "member@example.com"
        });
        database.Companies.Add(company);
        database.CompanyUsers.Add(CompanyUser.Create(userId, company.Id, CompanyUserRole.Owner, DateTimeOffset.UtcNow));
        await database.SaveChangesAsync();

        var reader = new SupportRequestContextReader(database);

        var context = await reader.GetAsync(userId, company.Id);
        var unrelatedContext = await reader.GetAsync(userId, Guid.NewGuid());

        Assert.Equal(new SupportRequestContext("Support member", "member@example.com", company.TradeName, company.LegalName), context);
        Assert.Null(unrelatedContext);
    }


    private static AppDbContext CreateDatabase()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    private static Company CreateCompany() => Company.Create(
        "VoltaMarcada Test Company Ltd.",
        "VoltaMarcada Test Company",
        "04.252.011/0001-10",
        "11999999999",
        "01001-000",
        "Praca da Se, 1",
        Guid.NewGuid(),
        Guid.NewGuid(),
        null,
        "support-test-company",
        DateTimeOffset.UtcNow);
}
