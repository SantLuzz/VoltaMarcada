using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Web.Identity;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Identity;

public sealed class CurrentCompanyTests
{
    [Fact]
    public async Task GetCompanyIdAsync_ShouldResolveCompanyId_WhenCalledConcurrently()
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<AppDbContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));

        await using var serviceProvider = services.BuildServiceProvider();
        var contextFactory = serviceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        var userId = Guid.NewGuid();
        var now = new DateTimeOffset(2026, 8, 18, 12, 0, 0, TimeSpan.Zero);
        var company = Company.Create(
            "VoltaMarcada Test Company Ltd.",
            "VoltaMarcada Test Company",
            "04.252.011/0001-10",
            "11999999999",
            "01001-000",
            "Praça da Sé, 1",
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            "servicesync-test-company",
            now);

        await using (var dbContext = await contextFactory.CreateDbContextAsync())
        {
            dbContext.Companies.Add(company);
            dbContext.CompanyUsers.Add(CompanyUser.Create(userId, company.Id, CompanyUserRole.Owner, now));
            await dbContext.SaveChangesAsync();
        }

        var currentCompany = new CurrentCompany(contextFactory, new TestCurrentUser(userId));

        var resolvedCompanyIds = await Task.WhenAll(
            Enumerable.Range(0, 10).Select(_ => currentCompany.GetCompanyIdAsync()));

        Assert.All(resolvedCompanyIds, companyId => Assert.Equal(company.Id, companyId));
    }

    private sealed class TestCurrentUser(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid UserId => userId;
    }
}
