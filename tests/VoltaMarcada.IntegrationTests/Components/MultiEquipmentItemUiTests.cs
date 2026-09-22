using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class MultiEquipmentItemUiTests
{
    [Fact]
    public void WorkOrderRegistration_ShouldPreserveEquipmentAndServiceForEachAdditionalItem()
    {
        var registrationSource = ReadSource("Components/Pages/WorkOrderRegistration.razor");
        var editorSource = ReadSource("Components/Shared/WorkOrderItemsEditor.razor");

        Assert.Contains("EquipmentOptions=\"equipment\"", registrationSource, StringComparison.Ordinal);
        Assert.Contains("ServiceOptions=\"services\"", registrationSource, StringComparison.Ordinal);
        Assert.Contains("BuildWorkOrderItems()", registrationSource, StringComparison.Ordinal);
        Assert.Contains("WorkOrderItemFormMapper.ToInputs(detail.Items)", registrationSource, StringComparison.Ordinal);
        Assert.Contains("Label=\"Equipamento\"", editorSource, StringComparison.Ordinal);
        Assert.Contains("Label=\"Servi\u00e7o\"", editorSource, StringComparison.Ordinal);
    }

    [Fact]
    public void QuoteRegistration_ShouldPreserveEquipmentAndServiceForEachItem()
    {
        var registrationSource = ReadSource("Components/Pages/QuoteRegistration.razor");

        Assert.Contains("Label=\"Equipamento\"", registrationSource, StringComparison.Ordinal);
        Assert.Contains("Label=\"Servi\u00e7o\"", registrationSource, StringComparison.Ordinal);
        Assert.Contains("new QuoteItemInput(item.EquipmentId, item.ServiceId", registrationSource, StringComparison.Ordinal);
        Assert.Contains("EquipmentId = item.EquipmentId", registrationSource, StringComparison.Ordinal);
        Assert.Contains("ServiceId = item.ServiceId", registrationSource, StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", relativePath));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
