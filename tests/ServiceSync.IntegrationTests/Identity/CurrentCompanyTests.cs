using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;
using ServiceSync.Infrastructure.Persistence;
using ServiceSync.Web.Identity;
using Xunit;

namespace ServiceSync.IntegrationTests.Identity;

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
            "ServiceSync Test Company Ltd.",
            "ServiceSync Test Company",
            "04.252.011/0001-10",
            "11999999999",
            "01001-000",
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
