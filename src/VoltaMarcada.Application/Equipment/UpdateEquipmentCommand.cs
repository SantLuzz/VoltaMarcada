namespace VoltaMarcada.Application.Equipment;

public sealed record UpdateEquipmentCommand(
    Guid CustomerId,
    string Type,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    string? Capacity,
    string? LocationDescription,
    DateOnly? InstallationDate,
    string? Notes) : IEquipmentCommand;
