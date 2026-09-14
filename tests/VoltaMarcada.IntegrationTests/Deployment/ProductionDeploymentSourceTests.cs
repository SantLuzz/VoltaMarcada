using System.Text.Json;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Deployment;

public sealed class ProductionDeploymentSourceTests
{
    [Fact]
    public void ProductionConfiguration_ShouldNotCommitSecretsAndMapHealthEndpoint()
    {
        var repositoryRoot = FindRepositoryRoot();
        var program = File.ReadAllText(Path.Combine(repositoryRoot, "src", "VoltaMarcada.Web", "Program.cs"));
        using var settingsDocument = JsonDocument.Parse(File.ReadAllText(Path.Combine(repositoryRoot, "src", "VoltaMarcada.Web", "appsettings.json")));
        var root = settingsDocument.RootElement;

        Assert.Contains("app.MapGet(\"/health\"", program);
        Assert.Contains("Results.Text(\"Healthy\")", program);
        Assert.Equal(string.Empty, root.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
        Assert.Equal(string.Empty, root.GetProperty("Cloudinary").GetProperty("ApiSecret").GetString());
        Assert.Equal(string.Empty, root.GetProperty("Smtp").GetProperty("Password").GetString());
        Assert.Equal(string.Empty, root.GetProperty("LicenseBilling").GetProperty("PixKey").GetString());
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return directory.FullName;
            }
        }

        throw new DirectoryNotFoundException("Repository root could not be found.");
    }
}
