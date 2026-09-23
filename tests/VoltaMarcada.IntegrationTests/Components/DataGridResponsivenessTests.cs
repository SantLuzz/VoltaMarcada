using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class DataGridResponsivenessTests
{
    [Fact]
    public void Shared_data_grid_allows_horizontal_scrolling_instead_of_hiding_columns()
    {
        var appCssPath = Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "wwwroot", "app.css");
        var css = File.ReadAllText(appCssPath);

        Assert.Matches(@"\.servicesync-data-grid \.mud-table-container \{\r?\n\s*overflow-x: auto;\r?\n\}", css);
        Assert.DoesNotMatch(@"\.servicesync-data-grid \.mud-table-container \{\r?\n\s*overflow-x: hidden;\r?\n\}", css);
    }

    [Fact]
    public void Platform_license_grid_keeps_company_and_contact_details_visible_on_mobile()
    {
        var repositoryRoot = FindRepositoryRoot();
        var page = File.ReadAllText(Path.Combine(repositoryRoot, "src", "VoltaMarcada.Web", "Components", "Pages", "PlatformLicenses.razor"));
        var css = File.ReadAllText(Path.Combine(repositoryRoot, "src", "VoltaMarcada.Web", "wwwroot", "app.css"));

        Assert.Contains("Class=\"servicesync-data-grid platform-license-data-grid\"", page, StringComparison.Ordinal);
        Assert.Contains("<GridTextCell Value=\"@context.CompanyName\" />", page, StringComparison.Ordinal);
        Assert.Matches(@"@media \(max-width: 959\.95px\) \{\s*\.platform-license-data-grid \.grid-text-cell", css);
        Assert.Contains("platform-license-data-grid .grid-text-cell", css, StringComparison.Ordinal);
    }

    [Fact]
    public void Global_mobile_layout_keeps_its_phone_only_breakpoint()
    {
        var css = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "wwwroot", "app.css"));

        Assert.Matches(@"@media \(max-width: 599\.95px\) \{\s*html, body", css);
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
