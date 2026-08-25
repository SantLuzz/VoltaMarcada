namespace VoltaMarcada.Domain.ValueObjects;

public sealed record WorkOrderChecklistItemDefinition(string Description, bool IsRequired, int DisplayOrder);
