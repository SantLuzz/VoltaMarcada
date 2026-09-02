using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using MudBlazor;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Components.Shared;
using Xunit;

#pragma warning disable BL0006

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class AppointmentEditorTests
{
    [Fact]
    public void VisitEditor_ShouldHideServiceAndRequireManualDuration()
    {
        var editor = new AppointmentEditor();
        SetField(editor, "isLoadingOptions", false);
        SetField(editor, "isOwner", true);
        SetField(editor, "appointmentType", AppointmentType.Visit);

        var frames = Render(editor);
        var selectLabels = FindComponentAttributes(frames, typeof(MudSelect<Guid?>))
            .Select(attributes => attributes.SingleOrDefault(attribute => attribute.AttributeName == "Label").AttributeValue as string)
            .ToArray();
        var numericLabels = FindComponentAttributes(frames, typeof(MudNumericField<int?>))
            .Select(attributes => attributes.SingleOrDefault(attribute => attribute.AttributeName == "Label").AttributeValue as string)
            .ToArray();

        Assert.DoesNotContain("Serviço", selectLabels);
        Assert.Contains("Duração (minutos)", numericLabels);
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
