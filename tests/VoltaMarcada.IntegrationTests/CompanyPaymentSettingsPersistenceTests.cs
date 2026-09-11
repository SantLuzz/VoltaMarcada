using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class CompanyPaymentSettingsPersistenceTests
{
    [Fact]
    public void CompanyPaymentSettings_AreConfiguredWithExpectedDatabaseConstraints()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var dbContext = new AppDbContext(options);
        var company = dbContext.Model.FindEntityType(typeof(Company));
        Assert.NotNull(company);

        Assert.Equal(255, company!.FindProperty(nameof(Company.PixKey))?.GetMaxLength());
        Assert.Equal(false, company.FindProperty(nameof(Company.PaymentControlEnabled))?.GetDefaultValue());
    }
}
