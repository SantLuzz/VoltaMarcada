using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class CompanyPaymentSettingsDomainTests
{
    [Fact]
    public void UpdatePaymentSettings_WhenPixKeyIsConfigured_EnablesPaymentControlAndStoresTheKey()
    {
        var company = Company.Create(
            "Volta Marcada Ltda.", "Volta Marcada", "12345678000195", "11999999999",
            "01001000", "Rua das Flores, 1", Guid.NewGuid(), Guid.NewGuid(), null, "volta-marcada",
            new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero));

        company.UpdatePaymentSettings(
            true,
            "financeiro@voltamarcada.com.br",
            PixKeyType.Email,
            new DateTimeOffset(2026, 9, 10, 12, 1, 0, TimeSpan.Zero));

        Assert.True(company.PaymentControlEnabled);
        Assert.Equal("financeiro@voltamarcada.com.br", company.PixKey);
        Assert.Equal(PixKeyType.Email, company.PixKeyType);
    }
}
