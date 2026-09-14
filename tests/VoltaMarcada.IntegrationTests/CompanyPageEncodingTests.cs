using Xunit;

namespace VoltaMarcada.IntegrationTests;

public sealed class PortugueseUiEncodingTests
{
    [Fact]
    public void CompanyAndWorkOrderPages_DoNotContainMojibakeText()
    {
        var repositoryRoot = FindRepositoryRoot();
        var companyPageContent = ReadPage(repositoryRoot, "Pages", "Company.razor");
        var workOrderPageContent = ReadPage(repositoryRoot, "Shared", "WorkOrderExecutionPanel.razor");

        Assert.DoesNotContain("Ã", companyPageContent, StringComparison.Ordinal);
        Assert.DoesNotContain("Ã", workOrderPageContent, StringComparison.Ordinal);
    }

    private static string ReadPage(string repositoryRoot, string directory, string fileName) =>
        File.ReadAllText(Path.Combine(repositoryRoot, "src", "VoltaMarcada.Web", "Components", directory, fileName));

    private static string FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "VoltaMarcada.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Repository root could not be found.");
    }
}
