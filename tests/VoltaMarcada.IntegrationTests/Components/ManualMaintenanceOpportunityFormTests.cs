using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class ManualMaintenanceOpportunityFormTests
{
    [Fact]
    public void Form_renders_required_manual_opportunity_fields()
    {
        var source = ReadWebSource("Components/Shared/ManualMaintenanceOpportunityForm.razor");

        Assert.Contains("Cliente", source, StringComparison.Ordinal);
        Assert.Contains("Equipamento", source, StringComparison.Ordinal);
        Assert.Contains("Servi\u00e7o recomendado", source, StringComparison.Ordinal);
        Assert.Contains("Data recomendada", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Form_loads_active_equipment_for_the_selected_customer_and_resets_stale_selection()
    {
        var source = ReadWebSource("Components/Shared/ManualMaintenanceOpportunityForm.razor");

        Assert.Contains("GetCustomerEquipmentUseCase", source, StringComparison.Ordinal);
        Assert.Contains("equipmentId = null", source, StringComparison.Ordinal);
        Assert.Contains(".Where(item => item.IsActive)", source, StringComparison.Ordinal);
    }

    private static string ReadWebSource(string relativePath) =>
        File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", relativePath));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
