using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class PlatformCompanyLicenseListTests
{
    [Fact]
    public async Task List_includes_a_company_license_without_a_payment()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var dbContext = new AppDbContext(options);
        var now = new DateTimeOffset(2026, 9, 9, 12, 0, 0, TimeSpan.Zero);
        var company = Company.Create("Acme Service Ltda", "Acme", "04.252.011/0001-10", "11999999999", "01001-000", "Praça da Sé, 1", Guid.NewGuid(), Guid.NewGuid(), null, "acme", now);
        var ownerId = Guid.NewGuid();
        dbContext.Companies.Add(company);
        dbContext.Users.Add(new ApplicationUser { Id = ownerId, UserName = "owner@acme.test", Email = "owner@acme.test", FullName = "Acme Owner" });
        dbContext.CompanyUsers.Add(CompanyUser.Create(ownerId, company.Id, CompanyUserRole.Owner, now));
        dbContext.CompanyLicenses.Add(CompanyLicense.Create(company.Id, now, 14));
        await dbContext.SaveChangesAsync();

        var result = await new GetPlatformLicensePaymentsUseCase(new LicenseRepository(dbContext)).ExecuteAsync();

        var license = Assert.Single(result);
        Assert.Equal("Acme", license.CompanyName);
        Assert.Equal("owner@acme.test", license.OwnerEmail);
        Assert.Equal(Guid.Empty, license.Id);
        Assert.Null(license.Status);
        Assert.Equal(CompanyLicenseStatus.Trial, license.LicenseStatus);
    }
}
