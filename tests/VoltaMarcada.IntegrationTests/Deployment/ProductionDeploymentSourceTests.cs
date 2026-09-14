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

    [Fact]
    public void ProductionTopology_ShouldExposeOnlyCaddyAndUsePrivatePostgreSql()
    {
        var repositoryRoot = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", "compose.production.yml"));
        var webDockerfile = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", "Dockerfile.web"));

        Assert.Contains("caddy:", compose);
        Assert.Contains("landing:", compose);
        Assert.Contains("web:", compose);
        Assert.Contains("postgres:", compose);
        Assert.Contains("\"80:80\"", compose);
        Assert.Contains("\"443:443\"", compose);
        Assert.Contains("http://localhost:8080/health", compose);
        Assert.Contains("pg_isready", compose);
        Assert.Contains("USER app", webDockerfile);

        var postgresService = compose.Substring(compose.IndexOf("  postgres:", StringComparison.Ordinal));
        Assert.DoesNotContain("ports:", postgresService);
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
