using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor.Services;
using VoltaMarcada.Web.Components.Pages;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class PlatformDashboardRenderingTests
{
    [Fact]
    public async Task Platform_dashboard_renders_platform_navigation_without_company_actions()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<PlatformDashboard>();
            await component.QuiescenceTask;
            return System.Net.WebUtility.HtmlDecode(component.ToHtmlString());
        });

        Assert.Contains("Administra\u00e7\u00e3o da plataforma", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/plataforma/licencas\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Clientes", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Agenda", html, StringComparison.Ordinal);
    }
}
