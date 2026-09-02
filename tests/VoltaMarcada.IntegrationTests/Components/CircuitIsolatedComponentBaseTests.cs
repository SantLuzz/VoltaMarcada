using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using VoltaMarcada.Web.Components;
using VoltaMarcada.Web.Components.Pages;
using VoltaMarcada.Web.Components.Shared;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class CircuitIsolatedComponentBaseTests
{
    [Fact]
    public void Dashboard_ShouldOwnItsServiceScope()
    {
        Assert.True(typeof(Dashboard).IsSubclassOf(typeof(CircuitIsolatedComponentBase)));
    }

    [Fact]
    public void ScheduleBlockRequestPanel_ShouldOwnItsServiceScope()
    {
        Assert.True(typeof(ScheduleBlockRequestPanel).IsSubclassOf(typeof(CircuitIsolatedComponentBase)));
    }

    [Fact]
    public void AppointmentEditor_ShouldOwnItsServiceScope()
    {
        Assert.True(typeof(AppointmentEditor).IsSubclassOf(typeof(CircuitIsolatedComponentBase)));
    }

    [Fact]
    public async Task RenderComponents_ShouldResolveDifferentScopedDependencies()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<ScopedMarker>();

        await using var serviceProvider = services.BuildServiceProvider();
        var loggerFactory = serviceProvider.GetRequiredService<ILoggerFactory>();
        await using var renderer = new HtmlRenderer(serviceProvider, loggerFactory);
        var resolvedIds = new List<Guid>();

        await renderer.Dispatcher.InvokeAsync(() =>
        {
            var parameters = ParameterView.FromDictionary(new Dictionary<string, object?>
            {
                [nameof(ScopeProbe.OnResolved)] = (Action<Guid>)resolvedIds.Add
            });

            renderer.BeginRenderingComponent<ScopeProbe>(parameters);
            renderer.BeginRenderingComponent<ScopeProbe>(parameters);
        });

        Assert.Equal(2, resolvedIds.Count);
        Assert.NotEqual(resolvedIds[0], resolvedIds[1]);
    }

    private sealed class ScopedMarker
    {
        public Guid Id { get; } = Guid.NewGuid();
    }

    private sealed class ScopeProbe : CircuitIsolatedComponentBase
    {
        [Parameter]
        public Action<Guid> OnResolved { get; set; } = default!;

        protected override void OnInitialized() => OnResolved(GetScopedService<ScopedMarker>().Id);

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
        }
    }
}
