using Microsoft.Extensions.Options;
using VoltaMarcada.Web.Configuration;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class SupportConfigurationTests
{
    [Theory]
    [InlineData("(67) 99999-1234", "https://wa.me/5567999991234")]
    [InlineData("+55 67 99999-1234", "https://wa.me/5567999991234")]
    public void WhatsAppUrl_NormalizesBrazilianNumbers(string input, string expected)
    {
        var configuration = new SupportConfiguration(Options.Create(new SupportOptions { WhatsAppNumber = input }));

        Assert.Equal(expected, configuration.WhatsAppUrl);
    }

    [Theory]
    [InlineData("")]
    [InlineData("123")]
    [InlineData("+1 202 555 0123")]
    public void WhatsAppUrl_ReturnsNullForUnsupportedNumbers(string input)
    {
        var configuration = new SupportConfiguration(Options.Create(new SupportOptions { WhatsAppNumber = input }));

        Assert.Null(configuration.WhatsAppUrl);
    }
}
