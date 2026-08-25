using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.WorkOrders;

internal static class WorkOrderFinancialItems
{
    public static bool TryCreate(
        Service service,
        decimal? submittedServiceUnitPrice,
        IReadOnlyList<WorkOrderItemInput> additionalItems,
        out IReadOnlyCollection<WorkOrderItemDefinition> items)
    {
        var serviceUnitPrice = service.PriceType == ServicePriceType.Fixed
            ? service.BasePrice
            : submittedServiceUnitPrice;

        if (serviceUnitPrice is null || serviceUnitPrice < 0)
        {
            items = [];
            return false;
        }

        var definitions = new List<WorkOrderItemDefinition>
        {
            new(service.Name, 1, serviceUnitPrice.Value, WorkOrderItemType.Service)
        };

        definitions.AddRange(additionalItems.Select(item =>
            new WorkOrderItemDefinition(item.Description, item.Quantity, item.UnitPrice, WorkOrderItemType.Additional)));

        items = definitions;
        return true;
    }
}
