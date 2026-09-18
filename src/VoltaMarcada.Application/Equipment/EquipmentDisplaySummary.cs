namespace VoltaMarcada.Application.Equipment;

public static class EquipmentDisplaySummary
{
    public static string GetCompactDescription(
        IReadOnlyCollection<Guid?> itemEquipmentIds,
        IReadOnlyDictionary<Guid, string> descriptions,
        string fallbackDescription)
    {
        var selectedIds = itemEquipmentIds
            .Where(id => id is { } value && value != Guid.Empty)
            .Select(id => id!.Value)
            .Distinct()
            .ToArray();

        return selectedIds.Length switch
        {
            0 => fallbackDescription,
            1 => descriptions.GetValueOrDefault(selectedIds[0], fallbackDescription),
            _ => $"{selectedIds.Length} equipamentos selecionados"
        };
    }
}
