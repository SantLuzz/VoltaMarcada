using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using VoltaMarcada.Application.Abstractions.Payments;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Authorization;
using VoltaMarcada.Web.Components.Pages;
using VoltaMarcada.Web.Operations;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class LicenseRenewalRenderingTests
{
    [Fact]
    public async Task Renewal_page_shows_pix_amount_manual_analysis_and_pending_status()
    {
        var renewal = new LicenseRenewalDto(
            49.90m,
            new PixPaymentPayload("000201-pix-copy-and-paste"),
            new LicensePaymentSubmissionDto(
                Guid.NewGuid(),
                49.90m,
                LicensePaymentStatus.PendingReview,
                new DateTimeOffset(2026, 9, 4, 15, 30, 0, TimeSpan.Zero)));
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddScoped<IComponentOperationExecutor, ComponentOperationExecutor>();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<LicenseRenewal>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(LicenseRenewal.InitialRenewal)] = renewal
                }));
            await component.QuiescenceTask;
            return System.Net.WebUtility.HtmlDecode(component.ToHtmlString());
        });

        Assert.Contains("PIX Copia e Cola", html, StringComparison.Ordinal);
        Assert.Contains("R$ 49,90", html, StringComparison.Ordinal);
        Assert.Contains("000201-pix-copy-and-paste", html, StringComparison.Ordinal);
        Assert.Contains("Comprovante enviado", html, StringComparison.Ordinal);
        Assert.Contains("análise manual", html, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("alt=\"QR Code PIX\"", html, StringComparison.Ordinal);
        Assert.DoesNotContain("pagamentos/", html, StringComparison.Ordinal);
    }

    [Fact]
    public void Blocked_account_requires_authentication_but_not_company_membership()
    {
        var attributes = typeof(BlockedAccount)
            .GetCustomAttributes(typeof(AuthorizeAttribute), inherit: true)
            .Cast<AuthorizeAttribute>()
            .ToArray();

        Assert.Contains(attributes, attribute => string.IsNullOrWhiteSpace(attribute.Policy));
        Assert.DoesNotContain(attributes, attribute => attribute.Policy == AuthorizationPolicies.CompanyMember);
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

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }
}
