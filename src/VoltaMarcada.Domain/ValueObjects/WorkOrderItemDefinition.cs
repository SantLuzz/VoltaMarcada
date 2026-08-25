using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Domain.ValueObjects;

public sealed record WorkOrderItemDefinition(string Description, decimal Quantity, decimal UnitPrice, WorkOrderItemType Type);
