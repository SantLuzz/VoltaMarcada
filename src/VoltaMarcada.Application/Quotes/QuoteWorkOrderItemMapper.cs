using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Quotes;

public static class QuoteWorkOrderItemMapper
{
    public static IReadOnlyList<WorkOrderItemDefinition> ToWorkOrderItemDefinitions(IEnumerable<QuoteItem> quoteItems) =>
        quoteItems
            .Select(item => new WorkOrderItemDefinition(
                item.Description,
                item.Quantity,
                item.UnitPrice,
                ToWorkOrderItemType(item.Type),
                item.EquipmentId,
                item.ServiceId))
            .ToArray();

    private static WorkOrderItemType ToWorkOrderItemType(QuoteItemType type) => type switch
    {
        QuoteItemType.Service => WorkOrderItemType.Service,
        QuoteItemType.Part => WorkOrderItemType.Part,
        QuoteItemType.Material => WorkOrderItemType.Material,
        QuoteItemType.Travel => WorkOrderItemType.Travel,
        _ => WorkOrderItemType.Additional
    };
}
