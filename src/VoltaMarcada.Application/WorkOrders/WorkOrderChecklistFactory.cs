using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class WorkOrderChecklistFactory(
    IServiceRepository serviceRepository,
    IEquipmentRepository equipmentRepository)
{
    public async Task<IReadOnlyList<WorkOrderChecklistItemDefinition>> CreateAsync(
        Guid companyId,
        IReadOnlyCollection<WorkOrderItemDefinition> workOrderItems,
        CancellationToken cancellationToken = default)
    {
        var serviceItems = workOrderItems
            .Where(item => item.Type == WorkOrderItemType.Service && item.ServiceId is not null && item.ServiceId != Guid.Empty)
            .ToArray();
        var identifyItems = serviceItems.Length > 1;
        var definitions = new List<WorkOrderChecklistItemDefinition>();

        foreach (var item in serviceItems)
        {
            var service = await serviceRepository.GetForCompanyAsync(item.ServiceId!.Value, companyId, cancellationToken);
            if (service is null)
            {
                continue;
            }

            var equipment = item.EquipmentId is { } equipmentId
                ? await equipmentRepository.GetForCompanyAsync(equipmentId, companyId, cancellationToken)
                : null;
            var prefix = identifyItems ? $"{service.Name} — {GetEquipmentDescription(equipment)}: " : string.Empty;

            foreach (var checklistItem in service.ChecklistItems.OrderBy(checklistItem => checklistItem.DisplayOrder))
            {
                definitions.Add(new WorkOrderChecklistItemDefinition(
                    $"{prefix}{checklistItem.Description}",
                    checklistItem.IsRequired,
                    definitions.Count));
            }
        }

        return definitions;
    }

    private static string GetEquipmentDescription(global::VoltaMarcada.Domain.Entities.Equipment? equipment) =>
        equipment is null
            ? "Equipment"
            : string.Join(" ", new[] { equipment.Type, equipment.Manufacturer, equipment.Model }.Where(value => !string.IsNullOrWhiteSpace(value)));
}
