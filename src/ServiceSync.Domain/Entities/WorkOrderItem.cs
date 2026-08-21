using ServiceSync.Domain.Exceptions;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Domain.Entities;

public sealed class WorkOrderItem
{
    private WorkOrderItem() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public decimal Quantity { get; private set; }
    public decimal UnitPrice { get; private set; }
    public WorkOrderItemType Type { get; private set; }
    public decimal TotalAmount => Quantity * UnitPrice;
    public int DisplayOrder { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    internal static WorkOrderItem Create(Guid companyId, Guid workOrderId, WorkOrderItemDefinition definition, int displayOrder, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || workOrderId == Guid.Empty) throw new DomainException("Company and work order IDs are required.");
        if (definition is null || string.IsNullOrWhiteSpace(definition.Description)) throw new DomainException("Item description is required.");
        if (definition.Quantity <= 0) throw new DomainException("Item quantity must be greater than zero.");
        if (definition.UnitPrice < 0) throw new DomainException("Item unit price cannot be negative.");
        if (!Enum.IsDefined(definition.Type)) throw new DomainException("Work order item type is invalid.");
        if (displayOrder < 0) throw new DomainException("Display order cannot be negative.");
        var utcNow = now.ToUniversalTime();
        return new WorkOrderItem { Id = Guid.NewGuid(), CompanyId = companyId, WorkOrderId = workOrderId, Description = definition.Description.Trim(), Quantity = definition.Quantity, UnitPrice = definition.UnitPrice, Type = definition.Type, DisplayOrder = displayOrder, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow };
    }
}
