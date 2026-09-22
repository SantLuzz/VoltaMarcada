using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class HelpSupportSubmissionTests
{
    [Fact]
    public void HelpPage_ShouldCreateSupportTicketsWithinTheAuthenticatedCircuit()
    {
        var source = File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", "Components", "Pages", "Help.razor"));

        Assert.Contains("@inject IComponentOperationExecutor OperationExecutor", source, StringComparison.Ordinal);
        Assert.DoesNotContain("@inject IHttpClientFactory", source, StringComparison.Ordinal);
        Assert.DoesNotContain("PostAsync(NavigationManager.ToAbsoluteUri(\"support/requests\")", source, StringComparison.Ordinal);
    }

    private static string FindRepositoryRoot()
    {
        for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory is not null; directory = directory.Parent)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln"))) return directory.FullName;
        }

        throw new DirectoryNotFoundException("Repository root was not found.");
    }
}
