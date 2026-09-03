using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class WorkOrderHistoryEntry
{
    private WorkOrderHistoryEntry() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid WorkOrderId { get; private set; }
    public WorkOrderHistoryEventType EventType { get; private set; }
    public Guid? ActorUserId { get; private set; }
    public string Description { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static WorkOrderHistoryEntry Create(Guid companyId, Guid workOrderId, WorkOrderHistoryEventType eventType, Guid? actorUserId, string description, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || workOrderId == Guid.Empty) throw new DomainException("Company and work order IDs are required.");
        if (!Enum.IsDefined(eventType)) throw new DomainException("Work order history event type is invalid.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length > 1000) throw new DomainException("Work order history description is required and cannot exceed 1000 characters.");
        return new WorkOrderHistoryEntry { Id = Guid.NewGuid(), CompanyId = companyId, WorkOrderId = workOrderId, EventType = eventType, ActorUserId = actorUserId is { } userId && userId != Guid.Empty ? userId : null, Description = description.Trim(), CreatedAtUtc = now.ToUniversalTime() };
    }
}
