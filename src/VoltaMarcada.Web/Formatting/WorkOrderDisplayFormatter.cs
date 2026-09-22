using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Web.Formatting;

public static class WorkOrderDisplayFormatter
{
    public static string FormatOption(WorkOrderListItem workOrder) =>
        $"OS #{workOrder.Id.ToString()[..8]} — {workOrder.CustomerName} — {workOrder.ServiceName}";
}
