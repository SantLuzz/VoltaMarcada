using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Formatting;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class WorkOrderEnhancementsComponentTests
{
    [Fact]
    public void WorkOrderScreen_ShouldOfferAttachmentsAndChronologicalHistory()
    {
        var executionSource = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");
        var registrationSource = ReadSource("Components/Pages/WorkOrderRegistration.razor");

        Assert.Contains("Anexos", executionSource, StringComparison.Ordinal);
        Assert.Contains("/attachments/{attachment.Id}/content", executionSource, StringComparison.Ordinal);
        Assert.Contains("Histórico da OS", registrationSource, StringComparison.Ordinal);
        Assert.Contains("detail.HistoryEntries", registrationSource, StringComparison.Ordinal);
    }

    private static string ReadSource(string relativePath) => File.ReadAllText(Path.Combine(FindRepositoryRoot(), "src", "VoltaMarcada.Web", relativePath));
    [Fact]
    public void WorkOrderScreen_ShouldFilterProfessionalServicesAndKeepPhotoUploadInPlace()
    {
        var registrationSource = ReadSource("Components/Pages/WorkOrderRegistration.razor");
        var editorSource = ReadSource("Components/Shared/WorkOrderItemsEditor.razor");
        var photoModuleSource = ReadSource("wwwroot/js/work-order-photo-staging.js");

        Assert.Contains("GetCurrentEmployeeProfessionalUseCase", registrationSource, StringComparison.Ordinal);
        Assert.Contains("ServiceIds.Contains(service.Id)", registrationSource, StringComparison.Ordinal);
        Assert.Contains("DiscountAmount", editorSource, StringComparison.Ordinal);
        Assert.DoesNotContain("window.location.assign", photoModuleSource, StringComparison.Ordinal);
        Assert.Contains("volta-marcada:work-order-photo-uploaded", photoModuleSource, StringComparison.Ordinal);
    }


    [Fact]
    public void WorkOrderScreen_ShouldSupportMultiplePaymentInstallmentsWhenCompletingService()
    {
        var executionSource = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");
        var cashSource = ReadSource("Components/Pages/Cash.razor");

        Assert.Contains("Este atendimento não terá cobrança", executionSource, StringComparison.Ordinal);
        Assert.Contains("!HasIncompleteRequiredChecklistItems", executionSource, StringComparison.Ordinal);
        Assert.Contains("PaymentInstallmentFormModel", executionSource, StringComparison.Ordinal);
        Assert.Contains("AddPaymentInstallment", executionSource, StringComparison.Ordinal);
        Assert.Contains("RemovePaymentInstallment", executionSource, StringComparison.Ordinal);
        Assert.Contains("RedistributePaymentInstallments", executionSource, StringComparison.Ordinal);
        Assert.Contains("<BrazilianCurrencyInput", executionSource, StringComparison.Ordinal);
        Assert.Contains("PaymentInstallmentsTotal > WorkOrder.TotalAmount", executionSource, StringComparison.Ordinal);
        Assert.Contains("HasInvalidPaymentInstallments", executionSource, StringComparison.Ordinal);
        Assert.Contains("As parcelas devem totalizar o valor da OS.", executionSource, StringComparison.Ordinal);
        Assert.Contains("<BrazilianCurrencyInput", cashSource, StringComparison.Ordinal);
        Assert.Contains("A confirmar", cashSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkOrderExecutionPanel_ShouldInitializeRecommendedServiceForEachEquipment()
    {
        var executionSource = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");

        Assert.Contains("recommendedServiceIds[equipment.Id] = WorkOrder.ServiceId;", executionSource, StringComparison.Ordinal);
    }

    [Fact]
    public void WorkOrderSelectors_ShouldIncludeTheShortWorkOrderNumber()
    {
        var workOrder = new WorkOrderListItem(
            Guid.Parse("cafc147b-0000-0000-0000-000000000000"),
            "Fulano de tal",
            "Ar condicionado",
            "Instalação",
            WorkOrderStatus.Pending,
            WorkOrderPriority.Normal,
            DateTimeOffset.UtcNow,
            null);

        Assert.Equal("OS #cafc147b — Fulano de tal — Instalação", WorkOrderDisplayFormatter.FormatOption(workOrder));
        Assert.Contains("WorkOrderDisplayFormatter.FormatOption(workOrder)", ReadSource("Components/Pages/Cash.razor"), StringComparison.Ordinal);
        Assert.Contains("WorkOrderDisplayFormatter.FormatOption(workOrder)", ReadSource("Components/Pages/Schedule.razor"), StringComparison.Ordinal);
        Assert.Contains("WorkOrderDisplayFormatter.FormatOption(workOrder)", ReadSource("Components/Shared/AppointmentEditor.razor"), StringComparison.Ordinal);
    }

    [Fact]
    public void WorkOrderExecutionPanel_ShouldGroupRecommendedServicesByEquipment()
    {
        var executionSource = ReadSource("Components/Shared/WorkOrderExecutionPanel.razor");

        Assert.Contains("maintenance-recommendation-equipment", executionSource, StringComparison.Ordinal);
    }

    [Fact]
    public void Schedule_ShouldUseSingleColumnAppointmentActionsOnMobile()
    {
        var scheduleSource = ReadSource("Components/Pages/Schedule.razor");
        var styles = ReadSource("wwwroot/app.css");

        Assert.Contains("agenda-appointment-list", scheduleSource, StringComparison.Ordinal);
        Assert.Contains("Breakpoint=\"Breakpoint.SmAndDown\"", scheduleSource, StringComparison.Ordinal);
        Assert.Contains("Class=\"agenda-appointment-actions\"", scheduleSource, StringComparison.Ordinal);
        Assert.Contains(".agenda-appointment-list .mud-list-item-icon", styles, StringComparison.Ordinal);
        Assert.Contains(".agenda-appointment-actions", styles, StringComparison.Ordinal);
        Assert.Contains(".agenda-request-actions { display: grid; gap: 8px; grid-template-columns: minmax(0, 1fr); }", styles, StringComparison.Ordinal);
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
