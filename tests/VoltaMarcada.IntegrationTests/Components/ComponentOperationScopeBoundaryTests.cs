using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class ComponentOperationScopeBoundaryTests
{
    [Theory]
    [InlineData("Pages/Dashboard.razor")]
    [InlineData("Pages/Customers.razor")]
    [InlineData("Pages/Equipment.razor")]
    [InlineData("Pages/Services.razor")]
    [InlineData("Pages/Professionals.razor")]
    [InlineData("Pages/WorkOrders.razor")]
    [InlineData("Pages/MaintenanceOpportunities.razor")]
    [InlineData("Pages/Quotes.razor")]
    [InlineData("Pages/Company.razor")]
    [InlineData("Pages/CustomerRegistration.razor")]
    [InlineData("Pages/EquipmentRegistration.razor")]
    [InlineData("Pages/ServiceRegistration.razor")]
    [InlineData("Pages/ProfessionalRegistration.razor")]
    [InlineData("Pages/WorkOrderRegistration.razor")]
    [InlineData("Pages/QuoteRegistration.razor")]
    [InlineData("Pages/Schedule.razor")]
    [InlineData("Pages/PublicCompany.razor")]
    [InlineData("Pages/PublicMaintenanceBooking.razor")]
    [InlineData("Pages/PublicQuote.razor")]
    [InlineData("Pages/PublicAppointmentRequestResponse.razor")]
    [InlineData("Pages/EmployeeInvitationAcceptance.razor")]
    [InlineData("Shared/AppointmentDetailsDialog.razor")]
    [InlineData("Shared/AppointmentEditor.razor")]
    [InlineData("Shared/ProfessionalAvailabilityPanel.razor")]
    [InlineData("Shared/ScheduleBlockRequestPanel.razor")]
    [InlineData("Shared/WorkOrderAppointmentsPanel.razor")]
    [InlineData("Shared/WorkOrderExecutionPanel.razor")]
    public void Component_ShouldResolveApplicationServicesThroughOperationExecutor(string fileName)
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
            "src", "VoltaMarcada.Web", "Components", fileName));
}
