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
    public void ProductionTopology_ShouldExposeOnlyCaddyAndUseManagedPostgreSql()
    {
        var repositoryRoot = FindRepositoryRoot();
        var compose = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", "compose.production.yml"));
        var environmentExample = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", ".env.production.example"));
        var webDockerfile = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", "Dockerfile.web"));
        var landingDockerfile = File.ReadAllText(Path.Combine(repositoryRoot, "deploy", "Dockerfile.landing"));

        Assert.Equal(["caddy", "landing", "web"], GetComposeServiceNames(compose));
        Assert.Contains("\"80:80\"", compose);
        Assert.Contains("\"443:443\"", compose);
        Assert.Contains("http://localhost:8080/health", compose);
        Assert.Contains("ConnectionStrings__DefaultConnection: ${EXTERNAL_DATABASE_CONNECTION_STRING}", compose);
        Assert.Contains("ApplicationUrls__ApplicationBaseUrl: ${APPLICATION_BASE_URL}", compose);
        Assert.Contains("ApplicationUrls__LandingPageUrl: ${LANDING_PAGE_URL}", compose);
        Assert.Contains("Cloudinary__CloudName: ${CLOUDINARY_CLOUD_NAME}", compose);
        Assert.Contains("Cloudinary__ApiKey: ${CLOUDINARY_API_KEY}", compose);
        Assert.Contains("Cloudinary__ApiSecret: ${CLOUDINARY_API_SECRET}", compose);
        Assert.Contains("Smtp__Enabled: ${SMTP_ENABLED}", compose);
        Assert.Contains("Smtp__Host: ${SMTP_HOST}", compose);
        Assert.Contains("Smtp__Port: ${SMTP_PORT}", compose);
        Assert.Contains("Smtp__UseTls: ${SMTP_USE_TLS}", compose);
        Assert.Contains("Smtp__SenderEmail: ${SMTP_SENDER_EMAIL}", compose);
        Assert.Contains("Smtp__SenderName: ${SMTP_SENDER_NAME}", compose);
        Assert.Contains("Smtp__Username: ${SMTP_USERNAME}", compose);
        Assert.Contains("Smtp__Password: ${SMTP_PASSWORD}", compose);
        Assert.Contains("PlatformAdministrator__Email: ${PLATFORM_ADMINISTRATOR_EMAIL}", compose);
        Assert.Contains("PlatformAdministrator__Password: ${PLATFORM_ADMINISTRATOR_PASSWORD}", compose);
        Assert.DoesNotContain("AppUrls__Public", compose);
        Assert.Contains("EXTERNAL_DATABASE_CONNECTION_STRING=", environmentExample);
        Assert.Contains("TLS", environmentExample);
        Assert.DoesNotContain("POSTGRES_", environmentExample);
        Assert.DoesNotContain("Host=", environmentExample);
        Assert.DoesNotContain("POSTGRES_", compose);
        Assert.DoesNotContain("postgres", compose.ToLowerInvariant());
        Assert.DoesNotContain("depends_on:", compose);
        Assert.DoesNotContain("backend:", compose);
        Assert.DoesNotContain("postgres-data", compose);
        Assert.Contains("USER app", webDockerfile);
        Assert.Contains("s|/run/nginx.pid|/tmp/nginx.pid|", landingDockerfile);
        Assert.Matches(@"EXPOSE 8080\r?\n(?:\r?\n)?ENTRYPOINT \[\""nginx\"", \""-g\"", \""daemon off;\""\]", landingDockerfile);

        Assert.Equal(["80:80", "443:443"], GetPortMappings(GetServiceBlock(compose, "caddy")));
        foreach (var serviceName in new[] { "landing", "web" })
        {
            Assert.Empty(GetPortMappings(GetServiceBlock(compose, serviceName)));
        }
    }

    private static string[] GetComposeServiceNames(string compose)
    {
        var lines = compose.Split('\n');
        var servicesStart = Array.FindIndex(lines, line => line.TrimEnd('\r') == "services:");
        Assert.NotEqual(-1, servicesStart);

        var serviceNames = new List<string>();
        for (var index = servicesStart + 1; index < lines.Length; index++)
        {
            var line = lines[index].TrimEnd('\r');
            if (IsComposeTopLevelSection(line))
            {
                break;
            }

            if (IsComposeServiceHeader(line))
            {
                serviceNames.Add(line.Trim().TrimEnd(':'));
            }
        }

        return [.. serviceNames];
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

    private static string[] GetPortMappings(string serviceBlock)
    {
        var portMappings = new List<string>();
        var isInPortsBlock = false;

        foreach (var line in serviceBlock.Split('\n'))
        {
            var trimmedLine = line.Trim();
            if (!isInPortsBlock)
            {
                isInPortsBlock = trimmedLine == "ports:";
                continue;
            }

            if (trimmedLine.StartsWith("- ", StringComparison.Ordinal))
            {
                portMappings.Add(trimmedLine[2..].Trim('"'));
                continue;
            }

            if (!string.IsNullOrWhiteSpace(trimmedLine))
            {
                break;
            }
        }

        return [.. portMappings];
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

    [Fact]
    public void GetPortMappings_ShouldReturnOnlyPortItemsForTheService()
    {
        const string service = """
              caddy:
                ports:
                  - "80:80"
                  - "443:443"
                volumes:
                  - caddy-data:/data
            """;

        Assert.Equal(["80:80", "443:443"], GetPortMappings(service));
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
