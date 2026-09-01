using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.RenderTree;
using MudBlazor;
using VoltaMarcada.Application.Abstractions.Routing;
using VoltaMarcada.Application.CompanySettings;
using VoltaMarcada.Application.EmployeeInvitations;
using VoltaMarcada.Application.PublicBooking;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Web.Components.Pages;
using Xunit;

#pragma warning disable BL0006

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class CompanyRenderingTests
{
    [Fact]
    public void BusinessHoursPickers_ShouldNotForwardFullWidthAsAnHtmlAttribute()
    {
        var component = new Company();
        SetPublicSettings(component);

        var frames = Render(component);
        var pickerAttributes = FindComponentAttributes(frames, typeof(MudTimePicker)).ToArray();

        Assert.True(
            pickerAttributes.Length == 2,
            $"Expected two time pickers, but rendered: {string.Join(", ", frames.Where(frame => frame.FrameType == RenderTreeFrameType.Component).Select(frame => frame.ComponentType?.Name))}");
        Assert.All(pickerAttributes, attributes => Assert.DoesNotContain(attributes, frame => frame.AttributeName == "FullWidth"));
    }

    [Fact]
    public void PhaseSevenDatePickers_ShouldNotForwardFullWidthAsAnHtmlAttribute()
    {
        var publicCompany = new PublicCompany();
        SetField(publicCompany, "isLoading", false);
        SetField(publicCompany, "company", new PublicCompanyPageDto(
            "Test Company",
            null,
            null,
            "11999999999",
            null,
            [new PublicBookingServiceDto(Guid.NewGuid(), "Maintenance", null, 60)]));

        var maintenanceBooking = new PublicMaintenanceBooking();
        SetField(maintenanceBooking, "isLoading", false);
        SetField(maintenanceBooking, "context", new ContextualMaintenanceBookingDto(
            "test-company",
            "Test Company",
            null,
            "11999999999",
            "Test Customer",
            "Test Equipment",
            Guid.NewGuid(),
            "Maintenance",
            60));

        var schedule = new Schedule();
        typeof(Schedule).GetProperty("TimeProvider", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(schedule, TimeProvider.System);
        SetField(schedule, "requestToSuggest", new AppointmentRequestDto(
            Guid.NewGuid(),
            "Test Customer",
            "11999999999",
            Guid.NewGuid(),
            "Maintenance",
            null,
            string.Empty,
            new DateOnly(2026, 8, 31),
            new TimeOnly(8, 0),
            60,
            VoltaMarcada.Domain.Enums.AppointmentRequestStatus.Pending,
            false,
            false,
            null,
            null,
            null,
            null,
            [],
            []));

        var frames = Render(publicCompany)
            .Concat(Render(maintenanceBooking))
            .Concat(Render(schedule))
            .ToArray();
        var pickerAttributes = FindComponentAttributes(frames, typeof(MudDatePicker)).ToArray();

        Assert.NotEmpty(pickerAttributes);
        Assert.All(pickerAttributes, attributes => Assert.DoesNotContain(attributes, frame => frame.AttributeName == "FullWidth"));
    }

    private static void SetPublicSettings(Company component)
    {
        typeof(Company).GetProperty("ApplicationUrlBuilder", BindingFlags.Instance | BindingFlags.NonPublic)!
            .SetValue(component, new TestApplicationUrlBuilder());
        SetField(component, "isLoading", false);
        SetField(component, "overview", new CompanyOverviewDto(
                "Test Company Ltd.",
                "Test Company",
                "04252011000110",
                "11999999999",
                "01001000",
                "Test Street, 1",
                "SP",
                "Sao Paulo",
                null,
                [],
                []));

        var modelType = typeof(Company).GetNestedType("PublicSettingsFormModel", BindingFlags.NonPublic)!;
        var hours = new[]
        {
            new CompanyBusinessHoursDto(DayOfWeek.Monday, true, new TimeOnly(8, 0), new TimeOnly(18, 0))
        };
        var model = Activator.CreateInstance(
            modelType,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
            binder: null,
            args: ["test-company", null, null, null, 60, hours],
            culture: null);

        SetField(component, "publicSettings", model);
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
            var frame = frames[index];
            if (frame.FrameType != RenderTreeFrameType.Component || frame.ComponentType != componentType)
            {
                continue;
            }

            yield return frames
                .Skip(index + 1)
                .TakeWhile(candidate => candidate.FrameType == RenderTreeFrameType.Attribute)
                .ToArray();
        }
    }

    private sealed class TestApplicationUrlBuilder : IApplicationUrlBuilder
    {
        public string LandingPageUrl => "https://example.test";

        public string BuildApplicationUrl(string relativePath) => $"https://example.test/{relativePath}";
    }

    private static void SetField(object target, string fieldName, object? value) =>
        target.GetType().GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(target, value);
}
