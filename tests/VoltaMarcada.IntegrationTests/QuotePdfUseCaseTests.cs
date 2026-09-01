using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Infrastructure.Persistence;
using VoltaMarcada.Infrastructure.Persistence.Repositories;
using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class QuotePdfUseCaseTests
{
    [Fact]
    public async Task GetQuotePdfProfileAsync_WhenCompanyExists_ReturnsAddressAndLocationText()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var now = new DateTimeOffset(2026, 9, 1, 12, 0, 0, TimeSpan.Zero);
        var state = State.Create(Guid.NewGuid(), "Sao Paulo", "SP");
        var city = City.Create(Guid.NewGuid(), state.Id, "Sao Paulo");
        var company = Company.Create(
            "Volta Marcada Ltda",
            "Volta Marcada",
            "11222333000181",
            "11999999999",
            "01001000",
            "Praca da Se, 1",
            state.Id,
            city.Id,
            null,
            "volta-marcada",
            now);

        await using var context = new AppDbContext(options);
        await context.States.AddAsync(state);
        await context.Cities.AddAsync(city);
        await context.Companies.AddAsync(company);
        await context.SaveChangesAsync();

        var profile = await new CompanyRepository(context).GetQuotePdfProfileAsync(company.Id);

        Assert.NotNull(profile);
        Assert.Equal("Volta Marcada", profile.TradeName);
        Assert.Equal("Sao Paulo", profile.CityName);
        Assert.Equal("SP", profile.StateAbbreviation);
    }
}
