using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class MobileCameraReliabilityTests
{
    [Fact]
    public void ServerAndWorkOrderPage_ShouldRetainCircuitAndExecutionTab()
    {
        Assert.Contains("DisconnectedCircuitRetentionPeriod", ReadSource("Program.cs"), StringComparison.Ordinal);
        Assert.Contains("ActivePanelIndex", ReadSource("Components/Pages/WorkOrderRegistration.razor"), StringComparison.Ordinal);
        Assert.DoesNotContain("location.reload()", ReadSource("Components/Layout/ReconnectModal.razor.js"), StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath) => File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", relativePath));

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
