using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Domain.ValueObjects;

public sealed record QuoteItemDefinition(string Description, decimal Quantity, decimal UnitPrice, QuoteItemType Type, Guid? EquipmentId = null, Guid? ServiceId = null);
