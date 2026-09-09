using Microsoft.AspNetCore.Components;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using VoltaMarcada.Web.Components.Layout;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class PlatformLayoutRenderingTests
{
    [Fact]
    public async Task Platform_layout_uses_the_same_drawer_structure_as_the_company_layout()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<AuthenticationStateProvider, MasterAuthenticationStateProvider>();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<PlatformLayout>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(LayoutComponentBase.Body)] = (RenderFragment)(builder => builder.AddContent(0, "Conteúdo"))
                }));
            await component.QuiescenceTask;
            return System.Net.WebUtility.HtmlDecode(component.ToHtmlString());
        });

        Assert.Contains("drawer-brand", html, StringComparison.Ordinal);
        Assert.Contains("drawer-logo", html, StringComparison.Ordinal);
        Assert.Contains("Abrir ou recolher menu", html, StringComparison.Ordinal);
        Assert.Contains("drawer-footer", html, StringComparison.Ordinal);
        Assert.Contains("href=\"/plataforma/licencas\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("Clientes", html, StringComparison.Ordinal);
    }
    private sealed class MasterAuthenticationStateProvider : AuthenticationStateProvider
    {
        public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
            Task.FromResult(new AuthenticationState(new ClaimsPrincipal(new ClaimsIdentity(
                [new Claim(ClaimTypes.Name, "Administrador master")],
                "Test"))));
    }
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/plataforma");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }
    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);

        public ValueTask<TValue> InvokeAsync<TValue>(
            string identifier,
            CancellationToken cancellationToken,
            object?[]? args) =>
            ValueTask.FromResult(default(TValue)!);
    }
}
