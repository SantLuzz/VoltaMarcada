using System.Text;
using System.Text.RegularExpressions;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class LandingPageSourceTests
{
    [Fact]
    public void LandingPage_ShouldPresentTheRecurringMaintenanceValueProposition()
    {
        var html = ReadLandingFile("index.html");

        Assert.Contains("<h1>O serviço termina. O cliente não.</h1>", html);
        Assert.Equal(1, Regex.Matches(html, "<h1", RegexOptions.CultureInvariant).Count);
        Assert.Contains("<main", html);
        Assert.Contains("id=\"inicio\"", html);
        Assert.Contains("id=\"como-funciona\"", html);
        Assert.Contains("id=\"recursos\"", html);
        Assert.Contains("id=\"preco\"", html);
        Assert.Contains("R$ 49,90", html);
        Assert.Contains("14 dias grátis", html);
        Assert.Contains("<details", html);
    }

    [Fact]
    public void LandingPage_ShouldUseTheApprovedAccountDestinationsAndPublicMetadata()
    {
        var html = ReadLandingFile("index.html");

        Assert.Contains("https://app.voltamarcada.com.br/account/register", html);
        Assert.Contains("https://app.voltamarcada.com.br/account/login", html);
        Assert.Contains("<meta name=\"viewport\"", html);
        Assert.Contains("<meta name=\"description\" content=\"Organize solicitações, agenda, orçamentos e ordens de serviço e saiba quando chamar seus clientes novamente para a próxima manutenção.\">", html);
        Assert.Contains("<link rel=\"canonical\" href=\"https://voltamarcada.com.br/\">", html);
        Assert.Contains("property=\"og:title\"", html);
        Assert.Contains("property=\"og:description\"", html);
        Assert.Contains("property=\"og:image\"", html);
        Assert.Contains("favicon.svg", html);
        Assert.DoesNotContain("localhost", html, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("127.0.0.1", html, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void LandingStyles_ShouldSupportReducedMotion()
    {
        var styles = ReadLandingFile("styles.css");

        Assert.Contains("@media (prefers-reduced-motion: reduce)", styles);
        Assert.Matches("transition:\\s*none|transition-duration:\\s*0", styles);
        Assert.Matches("animation:\\s*none|animation-duration:\\s*0", styles);
    }

    private static string ReadLandingFile(string fileName) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), "landing", fileName), Encoding.UTF8);

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root could not be found.");
    }
}
