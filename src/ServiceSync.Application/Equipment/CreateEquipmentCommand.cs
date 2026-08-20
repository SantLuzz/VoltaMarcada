namespace ServiceSync.Application.Equipment;

public sealed record CreateEquipmentCommand(
    Guid CustomerId,
    string Type,
    string? Manufacturer,
    string? Model,
    string? SerialNumber,
    string? Capacity,
    string? LocationDescription,
    DateOnly? InstallationDate,
    string? Notes) : IEquipmentCommand;
