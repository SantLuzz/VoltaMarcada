namespace VoltaMarcada.Application.WorkOrders;

public static class WorkOrderItemFormMapper
{
    public static List<WorkOrderItemInput> ToInputs(IReadOnlyList<WorkOrderItemDto> items) =>
        items
            .Select(item => new WorkOrderItemInput(
                item.EquipmentId,
                item.ServiceId,
                item.Description,
                item.Quantity,
                item.UnitPrice,
                item.Type))
            .ToList();
}
