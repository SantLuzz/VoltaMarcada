using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class ComponentOperationScopeBoundaryTests
{
    [Theory]
    [InlineData("Dashboard.razor")]
    [InlineData("Customers.razor")]
    [InlineData("Equipment.razor")]
    [InlineData("Services.razor")]
    [InlineData("Professionals.razor")]
    [InlineData("WorkOrders.razor")]
    [InlineData("MaintenanceOpportunities.razor")]
    [InlineData("Quotes.razor")]
    [InlineData("Company.razor")]
    public void InternalReadPage_ShouldResolveApplicationServicesThroughOperationExecutor(string fileName)
    {
        var source = File.ReadAllText(GetPagePath(fileName));

        Assert.Contains("@inject IComponentOperationExecutor OperationExecutor", source);
        Assert.DoesNotMatch("@inject\\s+\\S*(UseCase|Authorization)\\s+", source);
        Assert.DoesNotContain("@inherits CircuitIsolatedComponentBase", source);
    }

    private static string GetPagePath(string fileName) =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "..",
            "src", "VoltaMarcada.Web", "Components", "Pages", fileName));
}
