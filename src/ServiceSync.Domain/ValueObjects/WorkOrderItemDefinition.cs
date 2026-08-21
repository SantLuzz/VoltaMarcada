using ServiceSync.Domain.Enums;

namespace ServiceSync.Domain.ValueObjects;

public sealed record WorkOrderItemDefinition(string Description, decimal Quantity, decimal UnitPrice, WorkOrderItemType Type);
