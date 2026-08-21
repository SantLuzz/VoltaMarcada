using ServiceSync.Domain.Exceptions;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Domain.Entities;

public sealed class WorkOrderChecklistItem
{
    private WorkOrderChecklistItem() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public bool IsRequired { get; private set; }
    public int DisplayOrder { get; private set; }
    public bool IsCompleted { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }

    internal static WorkOrderChecklistItem Create(Guid companyId, Guid workOrderId, WorkOrderChecklistItemDefinition definition)
    {
        if (companyId == Guid.Empty || workOrderId == Guid.Empty) throw new DomainException("Company and work order IDs are required.");
        if (definition is null || string.IsNullOrWhiteSpace(definition.Description)) throw new DomainException("Checklist item description is required.");
        if (definition.DisplayOrder < 0) throw new DomainException("Display order cannot be negative.");
        return new WorkOrderChecklistItem { Id = Guid.NewGuid(), CompanyId = companyId, WorkOrderId = workOrderId, Description = definition.Description.Trim(), IsRequired = definition.IsRequired, DisplayOrder = definition.DisplayOrder };
    }

    internal void SetCompletion(bool isCompleted, DateTimeOffset now)
    {
        IsCompleted = isCompleted;
        CompletedAtUtc = isCompleted ? now.ToUniversalTime() : null;
    }
}
