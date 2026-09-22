using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using System.Reflection;
using VoltaMarcada.Web.Components.Shared;
using VoltaMarcada.Web.Operations;
using Xunit;

#pragma warning disable BL0006

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class AppointmentDetailsDialogRenderingTests
{
    [Fact]
    public async Task Appointment_details_dialog_renders_a_responsive_actions_container()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IComponentOperationExecutor, NoopOperationExecutor>();
        await using var serviceProvider = services.BuildServiceProvider();
        await using var renderer = new HtmlRenderer(
            serviceProvider,
            serviceProvider.GetRequiredService<ILoggerFactory>());

        AppointmentDetailsDialog? dialog = null;
        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var component = renderer.BeginRenderingComponent<AppointmentDetailsDialogHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?>
                {
                    [nameof(AppointmentDetailsDialogHost.Capture)] = (Action<AppointmentDetailsDialog>)(value => dialog = value)
                }));
            await component.QuiescenceTask;
        });

        Assert.Contains(Render(dialog!), frame =>
            frame.FrameType == RenderTreeFrameType.Attribute &&
            frame.AttributeName is "class" or "Class" &&
            frame.AttributeValue?.ToString() == "appointment-details-actions");
        Assert.Contains(Render(dialog!), frame =>
            frame.FrameType == RenderTreeFrameType.Attribute &&
            frame.AttributeName == "Breakpoint" &&
            Equals(frame.AttributeValue, Breakpoint.SmAndDown));
    }

    private sealed class AppointmentDetailsDialogHost : ComponentBase
    {
        [Parameter] public Action<AppointmentDetailsDialog>? Capture { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<AppointmentDetailsDialog>(0);
            builder.AddAttribute(1, nameof(AppointmentDetailsDialog.Visible), true);
            builder.AddComponentReferenceCapture(2, component => Capture?.Invoke((AppointmentDetailsDialog)component));
            builder.CloseComponent();
        }
    }

    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
        }
    }

    private sealed class NoopOperationExecutor : IComponentOperationExecutor
    {
        public Task ExecuteAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default)
            where TService : notnull => throw new NotSupportedException();

        public Task<TResult> ExecuteAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default)
            where TService : notnull => throw new NotSupportedException();
    }

    private static IReadOnlyList<RenderTreeFrame> Render(ComponentBase component)
    {
        var builder = new RenderTreeBuilder();
        component.GetType().GetMethod("BuildRenderTree", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(component, [builder]);
        return ExpandFragments(builder.GetFrames().Array[..builder.GetFrames().Count]);
    }

    private static IReadOnlyList<RenderTreeFrame> ExpandFragments(IEnumerable<RenderTreeFrame> frames)
    {
        var expanded = frames.ToList();
        foreach (var fragment in expanded
                     .Where(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                     .Select(frame => frame.AttributeValue)
                     .OfType<RenderFragment>()
                     .ToArray())
        {
            var builder = new RenderTreeBuilder();
            fragment(builder);
            expanded.AddRange(ExpandFragments(builder.GetFrames().Array[..builder.GetFrames().Count]));
        }

        return expanded;
    }
}
