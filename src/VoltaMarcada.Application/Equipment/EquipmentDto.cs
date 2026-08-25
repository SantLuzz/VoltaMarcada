namespace VoltaMarcada.Application.Equipment;

public sealed record EquipmentDto(
    Guid Id,
    Guid CustomerId,
    string CustomerName,
    string Type,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    string? Capacity,
    string? LocationDescription,
    DateOnly? InstallationDate,
    string? Notes,
    bool IsActive)
{
    internal static EquipmentDto FromEntity(global::VoltaMarcada.Domain.Entities.Equipment equipment, string customerName) =>
        new(
            equipment.Id,
            equipment.CustomerId,
            customerName,
            equipment.Type,
            equipment.Manufacturer,
            equipment.Model,
            equipment.SerialNumber,
            equipment.Capacity,
            equipment.LocationDescription,
            equipment.InstallationDate,
            equipment.Notes,
            equipment.IsActive);
}
