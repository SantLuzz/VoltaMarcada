using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using MudBlazor;
using VoltaMarcada.Web.Components.Shared;
using Xunit;

#pragma warning disable BL0006

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class ProfessionalAvailabilityPanelTests
{
    [Fact]
    public void Editor_ShouldRenderTheScheduleBlockActionOnItsOwnRow()
    {
        var panel = new ProfessionalAvailabilityPanel();
        SetField(panel, "isLoading", false);

        var stacks = FindComponentAttributes(Render(panel), typeof(MudStack)).ToArray();

        Assert.Contains(stacks, attributes => attributes.Any(attribute =>
            attribute.AttributeName == "Class" &&
            attribute.AttributeValue is string value &&
            value.Contains("professional-block-action", StringComparison.Ordinal)));
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
            if (frames[index].FrameType != RenderTreeFrameType.Component || frames[index].ComponentType != componentType)
            {
                continue;
            }

            yield return frames
                .Skip(index + 1)
                .TakeWhile(frame => frame.FrameType == RenderTreeFrameType.Attribute)
                .ToArray();
        }
    }

    private static void SetField(object target, string fieldName, object? value) =>
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
