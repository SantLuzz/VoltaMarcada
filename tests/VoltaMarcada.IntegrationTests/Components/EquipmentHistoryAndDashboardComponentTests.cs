using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class EquipmentHistoryAndDashboardComponentTests
{
    [Fact]
    public void EquipmentPage_ProvidesHistoryActionAndDrawer()
    {
        var source = File.ReadAllText(GetSourcePath("Components", "Pages", "Equipment.razor"));

        Assert.Contains("HistoryTooltipText", source, StringComparison.Ordinal);
        Assert.Contains("<EquipmentHistoryDrawer", source, StringComparison.Ordinal);
    }

    [Fact]
    public void EquipmentHistoryDrawer_UsesLazyLoadingAndShowsRecordCount()
    {
        var source = File.ReadAllText(GetSourcePath("Components", "Shared", "EquipmentHistoryDrawer.razor"));

        Assert.Contains("HandleTimelineScrollAsync", source, StringComparison.Ordinal);
        Assert.Contains("isNearBottom", source, StringComparison.Ordinal);
        Assert.Contains("@historyRecords.Count de @historyPage.TotalRecordCount registros", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Dashboard_UsesOwnerDashboardProjectionForOwnerOnlyManagementSurface()
    {
        var source = File.ReadAllText(GetSourcePath("Components", "Pages", "Dashboard.razor"));

        Assert.Contains("GetOwnerDashboardUseCase", source, StringComparison.Ordinal);
        Assert.Contains("O que precisa da sua atenção hoje", source, StringComparison.Ordinal);
        Assert.Contains("Receita recuperada", source, StringComparison.Ordinal);
        Assert.Contains("if (isOwner)", source, StringComparison.Ordinal);
    }

    private static string GetSourcePath(params string[] segments)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return Path.Combine(directory.FullName, "src", "VoltaMarcada.Web", Path.Combine(segments));
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("The solution root could not be located.");
    }
}
