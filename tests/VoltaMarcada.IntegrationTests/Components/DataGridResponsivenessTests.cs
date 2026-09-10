using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class DataGridResponsivenessTests
{
    [Fact]
    public void Shared_data_grid_allows_horizontal_scrolling_instead_of_hiding_columns()
    {
        var appCssPath = Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "wwwroot", "app.css");
        var css = File.ReadAllText(appCssPath);

        Assert.Contains(".servicesync-data-grid .mud-table-container {\r\n    overflow-x: auto;\r\n}", css, StringComparison.Ordinal);
        Assert.DoesNotContain(".servicesync-data-grid .mud-table-container {\r\n    overflow-x: hidden;\r\n}", css, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        var configuredRoot = Environment.GetEnvironmentVariable("VOLTAMARCADA_REPOSITORY_ROOT");
        if (!string.IsNullOrWhiteSpace(configuredRoot) && File.Exists(Path.Combine(configuredRoot, "VoltaMarcada.sln")))
        {
            return configuredRoot;
        }

        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
        {
            directory = directory.Parent;
        }

        return directory?.FullName ?? throw new DirectoryNotFoundException("Could not locate the repository root.");
    }
}
