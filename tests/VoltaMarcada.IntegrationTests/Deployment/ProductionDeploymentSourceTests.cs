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
        var landingDockerfile = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", "Dockerfile.landing"));

        Assert.Contains("caddy:", compose);
        Assert.Contains("landing:", compose);
        Assert.Contains("web:", compose);
        Assert.Contains("postgres:", compose);
        Assert.Contains("\"80:80\"", compose);
        Assert.Contains("\"443:443\"", compose);
        Assert.Contains("http://localhost:8080/health", compose);
        Assert.Contains("pg_isready", compose);
        Assert.Contains("USER app", webDockerfile);
        Assert.Matches(@"EXPOSE 8080\r?\n(?:\r?\n)?ENTRYPOINT \[\""nginx\"", \""-g\"", \""daemon off;\""\]", landingDockerfile);

        foreach (var serviceName in new[] { "landing", "web", "postgres" })
        {
            Assert.DoesNotContain("ports:", GetServiceBlock(compose, serviceName));
        }
    }

    private static string GetServiceBlock(string compose, string serviceName)
    {
        var serviceMarker = $"  {serviceName}:";
        var lines = compose.Split('\n');
        var serviceStart = Array.FindIndex(lines, line => line.TrimEnd('\r') == serviceMarker);
        Assert.NotEqual(-1, serviceStart);

        var serviceLines = new List<string> { lines[serviceStart].TrimEnd('\r') };
        for (var index = serviceStart + 1; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            if (IsComposeServiceHeader(line) || IsComposeTopLevelSection(line))
            {
                break;
            }

            serviceLines.Add(line);
        }

        return string.Join('\n', serviceLines);
    }

    private static bool IsComposeServiceHeader(string line)
    {
        return line.Length > 3
            && line.StartsWith("  ", StringComparison.Ordinal)
            && !char.IsWhiteSpace(line[2])
            && line.EndsWith(':')
            && line.IndexOf(':') == line.Length - 1;
    }

    private static bool IsComposeTopLevelSection(string line)
    {
        return line.Length > 0
            && !char.IsWhiteSpace(line[0])
            && line.EndsWith(':');
    }

    [Fact]
    public void GetServiceBlock_ShouldIncludeNestedPropertiesUntilTheNextServiceHeader()
    {
        const string compose = """
            services:
              landing:
                image: voltamarcada-landing:0.0.0
                ports:
                  - "8080:8080"
              web:
                image: voltamarcada-web:0.0.0
            """;

        var landingService = GetServiceBlock(compose, "landing");

        Assert.Contains("image: voltamarcada-landing:0.0.0", landingService);
        Assert.Contains("ports:", landingService);
        Assert.DoesNotContain("web:", landingService);
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
