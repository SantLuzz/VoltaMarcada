using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Components.Pages;
using VoltaMarcada.Web.Formatting;
using VoltaMarcada.Web.Operations;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class MaintenanceOpportunitiesRenderingTests
{
    [Fact]
    public async Task Page_renders_manual_history_label_for_a_manual_opportunity_projection()
    {
        var opportunity = new MaintenanceOpportunityListItem(Guid.NewGuid(), "Cliente da manuten\u00e7\u00e3o", "11999999999", "Ar-condicionado \u00b7 Carrier \u00b7 42Q", null, "Higieniza\u00e7\u00e3o", new DateOnly(2026, 12, 8), "Retorno preventivo.", MaintenanceOpportunityStatus.Pending, MaintenanceOpportunityUrgency.Future, null, null, null, null, null, null, []);
        var executor = new MaintenanceOpportunitiesOperationExecutor(opportunity);
        await using var provider = CreateServiceProvider(executor);
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());

        var html = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<MaintenanceOpportunities>();
            await component.QuiescenceTask;
            return System.Net.WebUtility.HtmlDecode(component.ToHtmlString());
        });

        Assert.Equal(1, executor.SummaryRequests);
        Assert.Equal(1, executor.ListRequests);
        Assert.Contains("Cliente da manuten\u00e7\u00e3o", html, StringComparison.Ordinal);
        Assert.Contains("Sem OS anterior", html, StringComparison.Ordinal);
        Assert.DoesNotContain("\u00daltima manuten\u00e7\u00e3o:", html, StringComparison.Ordinal);
    }

    private static ServiceProvider CreateServiceProvider(MaintenanceOpportunitiesOperationExecutor executor)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IComponentOperationExecutor>(executor);
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        services.AddSingleton<ICurrentDisplayTimeZone, UtcDisplayTimeZone>();
        services.AddSingleton<IBrazilianDisplayFormatter, BrazilianDisplayFormatter>();
        return services.BuildServiceProvider();
    }

    private sealed class MaintenanceOpportunitiesOperationExecutor(MaintenanceOpportunityListItem opportunity) : IComponentOperationExecutor
    {
        public int SummaryRequests { get; private set; }
        public int ListRequests { get; private set; }

        public Task ExecuteAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : notnull => throw new NotSupportedException();

        public Task<TResult> ExecuteAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : notnull
        {
            object result = typeof(TService) switch
            {
                var type when type == typeof(GetMaintenanceOpportunitySummaryUseCase) => GetSummary(),
                var type when type == typeof(GetMaintenanceOpportunitiesUseCase) => GetList(),
                _ => throw new NotSupportedException($"Unexpected component operation: {typeof(TService).Name}.")
            };

            return Task.FromResult((TResult)result);
        }

        private OperationResult<MaintenanceOpportunitySummary> GetSummary()
        {
            SummaryRequests++;
            return OperationResult<MaintenanceOpportunitySummary>.Success(new MaintenanceOpportunitySummary(0, 0, 0, 0));
        }

        private OperationResult<IReadOnlyList<MaintenanceOpportunityListItem>> GetList()
        {
            ListRequests++;
            return OperationResult<IReadOnlyList<MaintenanceOpportunityListItem>>.Success([opportunity]);
        }
    }

    private sealed class UtcDisplayTimeZone : ICurrentDisplayTimeZone
    {
        public Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken = default) => Task.FromResult(TimeZoneInfo.Utc);
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");
        protected override void NavigateToCore(string uri, NavigationOptions options) { }
    }

    private sealed class NoopJsRuntime : IJSRuntime
    {
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object?[]? args) => ValueTask.FromResult(default(TValue)!);
        public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object?[]? args) => ValueTask.FromResult(default(TValue)!);
    }
}
