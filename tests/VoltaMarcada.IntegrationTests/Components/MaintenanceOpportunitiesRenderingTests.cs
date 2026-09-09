using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class MaintenanceOpportunitiesRenderingTests
{
    [Fact]
    public void Page_renders_manual_history_label_when_work_order_is_missing()
    {
        var source = ReadWebSource("Components/Pages/MaintenanceOpportunities.razor");

        Assert.Contains("Sem OS anterior", source, StringComparison.Ordinal);
    }

    [Fact]
    public void Page_offers_manual_opportunity_creation_and_reloads_after_success()
    {
        var source = ReadWebSource("Components/Pages/MaintenanceOpportunities.razor");

        Assert.Contains("ManualMaintenanceOpportunityForm", source, StringComparison.Ordinal);
        Assert.Contains("OnCreated=\"LoadAsync\"", source, StringComparison.Ordinal);
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
