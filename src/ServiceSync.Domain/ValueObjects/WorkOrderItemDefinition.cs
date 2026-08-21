namespace ServiceSync.Domain.ValueObjects;

public sealed record WorkOrderItemDefinition(string Description, decimal Quantity, decimal UnitPrice);
