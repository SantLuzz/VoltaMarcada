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

    public static bool TryCreate(IReadOnlyList<WorkOrderItemInput> submittedItems, out IReadOnlyCollection<WorkOrderItemDefinition> items)
    {
        if (submittedItems.Count is 0 or > 100 || submittedItems.Any(item => string.IsNullOrWhiteSpace(item.Description) || item.Description.Length > 500 || item.Quantity <= 0 || item.UnitPrice < 0 || !Enum.IsDefined(item.Type) || item.Type == WorkOrderItemType.Service && (item.ServiceId is null || item.ServiceId == Guid.Empty)))
        {
            items = [];
            return false;
        }

        items = submittedItems.Select(item => new WorkOrderItemDefinition(item.Description, item.Quantity, item.UnitPrice, item.Type, item.EquipmentId, item.ServiceId)).ToArray();
        return true;
    }
}
