using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MudBlazor;
using MudBlazor.Services;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Application.Equipment;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Application.Services;
using VoltaMarcada.Web.Components.Shared;
using VoltaMarcada.Web.Operations;
using Xunit;

#pragma warning disable BL0006

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class ManualMaintenanceOpportunityFormTests
{
    [Fact]
    public async Task Form_renders_required_manual_opportunity_controls_after_opening()
    {
        var executor = new FormOperationExecutor();
        await using var provider = CreateServiceProvider(executor);
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        ManualMaintenanceOpportunityForm? form = null;
        var component = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = renderer.BeginRenderingComponent<ManualMaintenanceOpportunityFormHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(ManualMaintenanceOpportunityFormHost.Capture)] = (Action<ManualMaintenanceOpportunityForm>)(value => form = value) }));
            await root.QuiescenceTask;
            return root;
        });

        await renderer.Dispatcher.InvokeAsync(() => InvokeTaskAsync(form!, "OpenCreateDialog"));
        var html = await renderer.Dispatcher.InvokeAsync(() => component.ToHtmlString());
        var fields = FindComponentAttributes(Render(form!), typeof(MudSelect<Guid?>)).ToArray();
        var labels = fields.Select(attributes => attributes.Single(attribute => attribute.AttributeName == "Label").AttributeValue as string).ToArray();

        Assert.Contains("Nova oportunidade", html, StringComparison.Ordinal);
        Assert.Contains("Cliente", labels);
        Assert.Contains("Equipamento", labels);
        Assert.Contains("Servi\u00e7o recomendado", labels);
        Assert.All(fields, attributes => Assert.Contains(attributes, attribute => attribute.AttributeName == "Required" && Equals(attribute.AttributeValue, true)));
        Assert.Contains(FindComponentAttributes(Render(form!), typeof(MudSelect<string>)), attributes => attributes.Any(attribute => attribute.AttributeName == "Label" && Equals(attribute.AttributeValue, "Data recomendada")));
    }

    [Fact]
    public async Task Form_loads_active_equipment_and_clears_the_stale_selection_when_customer_changes()
    {
        var customerId = Guid.NewGuid();
        var staleEquipmentId = Guid.NewGuid();
        var activeEquipment = new EquipmentDto(Guid.NewGuid(), customerId, "Cliente da manuten\u00e7\u00e3o", "Ar-condicionado", "Carrier", "42Q", null, null, null, null, null, true);
        var inactiveEquipment = new EquipmentDto(Guid.NewGuid(), customerId, "Cliente da manuten\u00e7\u00e3o", "Ar-condicionado", "Carrier", "Inativo", null, null, null, null, null, false);
        var executor = new FormOperationExecutor([activeEquipment, inactiveEquipment]);
        await using var provider = CreateServiceProvider(executor);
        await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
        ManualMaintenanceOpportunityForm? form = null;
        var component = await renderer.Dispatcher.InvokeAsync(async () =>
        {
            var root = renderer.BeginRenderingComponent<ManualMaintenanceOpportunityFormHost>(
                ParameterView.FromDictionary(new Dictionary<string, object?> { [nameof(ManualMaintenanceOpportunityFormHost.Capture)] = (Action<ManualMaintenanceOpportunityForm>)(value => form = value) }));
            await root.QuiescenceTask;
            return root;
        });

        await renderer.Dispatcher.InvokeAsync(async () =>
        {
            await InvokeTaskAsync(form!, "OpenCreateDialog");
            SetPrivateField(form!, "customerId", customerId);
            SetPrivateField(form!, "equipmentId", staleEquipmentId);
            await InvokeTaskAsync(form!, "LoadEquipmentAsync");
        });
        var frames = Render(form!);

        Assert.Equal(1, executor.EquipmentRequests);
        Assert.Null(GetPrivateField<Guid?>(form!, "equipmentId"));
        Assert.Contains(frames, frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent == "Ar-condicionado \u00b7 Carrier \u00b7 42Q");
        Assert.DoesNotContain(frames, frame => frame.FrameType == RenderTreeFrameType.Text && frame.TextContent!.Contains("Inativo", StringComparison.Ordinal));
    }

    private static ServiceProvider CreateServiceProvider(FormOperationExecutor executor)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddMudServices();
        services.AddSingleton<NavigationManager, TestNavigationManager>();
        services.AddSingleton<IComponentOperationExecutor>(executor);
        return services.BuildServiceProvider();
    }

    private static async Task InvokeTaskAsync(object target, string methodName)
    {
        await ((Task)target.GetType().GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(target, null)!);
        typeof(ComponentBase).GetMethod("StateHasChanged", BindingFlags.Instance | BindingFlags.NonPublic)!
            .Invoke(target, null);
    }

    private static void SetPrivateField(object target, string fieldName, object? value) =>
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);

    private static T GetPrivateField<T>(object target, string fieldName) =>
        (T)target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;

    private static int CountOccurrences(string value, string needle)
    {
        var count = 0;
        var position = 0;
        while ((position = value.IndexOf(needle, position, StringComparison.OrdinalIgnoreCase)) >= 0)
        {
            count++;
            position += needle.Length;
        }

        return count;
    }

    private sealed class ManualMaintenanceOpportunityFormHost : ComponentBase
    {
        [Parameter] public Action<ManualMaintenanceOpportunityForm>? Capture { get; set; }

        protected override void BuildRenderTree(RenderTreeBuilder builder)
        {
            builder.OpenComponent<ManualMaintenanceOpportunityForm>(0);
            builder.AddComponentReferenceCapture(1, component => Capture?.Invoke((ManualMaintenanceOpportunityForm)component));
            builder.CloseComponent();
        }
    }
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager() => Initialize("https://localhost/", "https://localhost/");
        protected override void NavigateToCore(string uri, NavigationOptions options) { }
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

    private static IEnumerable<IReadOnlyList<RenderTreeFrame>> FindComponentAttributes(
        IReadOnlyList<RenderTreeFrame> frames,
        Type componentType)
    {
        for (var index = 0; index < frames.Count; index++)
        {
            if (frames[index].FrameType == RenderTreeFrameType.Component && frames[index].ComponentType == componentType)
            {
                yield return frames.Skip(index + 1).TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute).ToArray();
            }
        }
    }

    private sealed class FormOperationExecutor(IReadOnlyList<EquipmentDto>? equipment = null) : IComponentOperationExecutor
    {
        private static readonly IReadOnlyList<CustomerDto> Customers = [new(Guid.NewGuid(), "Cliente da manuten\u00e7\u00e3o", "12345678909", "11999999999", null, null, true, [])];
        private static readonly IReadOnlyList<ServiceDto> Services = [new(Guid.NewGuid(), "Higieniza\u00e7\u00e3o", null, 60, VoltaMarcada.Domain.Enums.ServicePriceType.Hidden, null, false, false, false, true, [])];
        private readonly IReadOnlyList<EquipmentDto> equipment = equipment ?? [];

        public int EquipmentRequests { get; private set; }

        public Task ExecuteAsync<TService>(Func<TService, CancellationToken, Task> operation, CancellationToken cancellationToken = default) where TService : notnull => throw new NotSupportedException();

        public Task<TResult> ExecuteAsync<TService, TResult>(Func<TService, CancellationToken, Task<TResult>> operation, CancellationToken cancellationToken = default) where TService : notnull
        {
            object result = typeof(TService) switch
            {
                var type when type == typeof(GetCustomersUseCase) => Customers,
                var type when type == typeof(GetServicesUseCase) => Services,
                var type when type == typeof(GetCustomerEquipmentUseCase) => GetEquipment(),
                _ => throw new NotSupportedException($"Unexpected component operation: {typeof(TService).Name}.")
            };

            return Task.FromResult((TResult)result);
        }

        private IReadOnlyList<EquipmentDto> GetEquipment()
        {
            EquipmentRequests++;
            return equipment;
        }
    }
}
