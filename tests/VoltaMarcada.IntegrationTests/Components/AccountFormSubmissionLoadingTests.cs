using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using MudBlazor.Services;
using VoltaMarcada.Web.Components.Pages.Account;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class AccountFormSubmissionLoadingTests
{
    [Fact]
    public async Task Login_form_enables_submission_loading_for_valid_post()
    {
        var html = await RenderAsync<Login>();

        Assert.Contains("<form method=\"post\"", html, StringComparison.Ordinal);
        Assert.Contains("data-submission-loading=\"true\"", html, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Register_form_enables_submission_loading_for_valid_post()
    {
        var html = await RenderAsync<Register>();

        Assert.Contains("<form method=\"post\"", html, StringComparison.Ordinal);
        Assert.Contains("data-submission-loading=\"true\"", html, StringComparison.Ordinal);
    }

    private static async Task<string> RenderAsync<TComponent>() where TComponent : class, IComponent
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<IJSRuntime, NoopJsRuntime>();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());

        return await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<TComponent>();
            await component.QuiescenceTask;
            return component.ToHtmlString();
        });
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
