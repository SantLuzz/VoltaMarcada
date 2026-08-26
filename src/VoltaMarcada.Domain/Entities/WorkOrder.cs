using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class WorkOrder : ICompanyScopedEntity
{
    private readonly List<WorkOrderItem> _items = [];
    private readonly List<WorkOrderChecklistItem> _checklistItems = [];
    private readonly List<WorkOrderPhoto> _photos = [];

    private WorkOrder() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid? EquipmentId { get; private set; }
    public Guid ServiceId { get; private set; }
    public Guid? ProfessionalId { get; private set; }
    public Guid CreatedByUserId { get; private set; }
    public WorkOrderPriority Priority { get; private set; }
    public string? ReportedProblem { get; private set; }
    public string? ExecutionNotes { get; private set; }
    public Guid? SourceQuoteId { get; private set; }
    public decimal DiscountAmount { get; private set; }
    public WorkOrderStatus Status { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public DateTimeOffset? StartedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public IReadOnlyCollection<WorkOrderItem> Items => _items;
    public IReadOnlyCollection<WorkOrderChecklistItem> ChecklistItems => _checklistItems;
    public IReadOnlyCollection<WorkOrderPhoto> Photos => _photos;
    public decimal TotalAmount => _items.Sum(item => item.TotalAmount) - DiscountAmount;

    public static WorkOrder Create(Guid companyId, Guid customerId, Guid? equipmentId, Guid serviceId, Guid? professionalId, Guid createdByUserId, WorkOrderPriority priority, string? reportedProblem, IReadOnlyCollection<WorkOrderItemDefinition> items, IReadOnlyCollection<WorkOrderChecklistItemDefinition> checklistItems, DateTimeOffset now, Guid? sourceQuoteId = null, decimal discountAmount = 0)
    {
        RequireId(companyId, "Company"); RequireId(customerId, "Customer"); RequireId(serviceId, "Service"); RequireId(createdByUserId, "Creator");
        if (discountAmount < 0) throw new DomainException("Work order discount cannot be negative.");
        if (!Enum.IsDefined(priority)) throw new DomainException("Work order priority is invalid.");
        var utcNow = now.ToUniversalTime();
        var workOrder = new WorkOrder { Id = Guid.NewGuid(), CompanyId = companyId, CustomerId = customerId, EquipmentId = NormalizeOptionalId(equipmentId), ServiceId = serviceId, ProfessionalId = professionalId, CreatedByUserId = createdByUserId, Priority = priority, ReportedProblem = NormalizeOptionalText(reportedProblem), SourceQuoteId = NormalizeOptionalId(sourceQuoteId), DiscountAmount = discountAmount, Status = WorkOrderStatus.Pending, CreatedAtUtc = utcNow, UpdatedAtUtc = utcNow };
        workOrder.ReplaceItems(items, utcNow);
        workOrder.ReplaceChecklistItems(checklistItems);
        return workOrder;
    }

    public void UpdatePendingDetails(Guid customerId, Guid equipmentId, Guid serviceId, Guid? professionalId, WorkOrderPriority priority, string? reportedProblem, IReadOnlyCollection<WorkOrderItemDefinition> items, IReadOnlyCollection<WorkOrderChecklistItemDefinition> checklistItems, DateTimeOffset now)
    {
        EnsurePending(); RequireId(customerId, "Customer"); RequireId(serviceId, "Service");
        if (serviceId != ServiceId) throw new DomainException("Work order service cannot be changed.");
        if (items is null) throw new DomainException("Work order items are required.");
        var existingServiceItem = _items.Single(item => item.Type == WorkOrderItemType.Service);
        var submittedServiceItem = items.SingleOrDefault(item => item.Type == WorkOrderItemType.Service);
        if (submittedServiceItem is null ||
            !string.Equals(submittedServiceItem.Description.Trim(), existingServiceItem.Description, StringComparison.Ordinal) ||
            submittedServiceItem.Quantity != existingServiceItem.Quantity ||
            submittedServiceItem.UnitPrice != existingServiceItem.UnitPrice)
        {
            throw new DomainException("Work order service item cannot be changed.");
        }
        if (!Enum.IsDefined(priority)) throw new DomainException("Work order priority is invalid.");
        CustomerId = customerId; EquipmentId = NormalizeOptionalId(equipmentId); ServiceId = serviceId; ProfessionalId = professionalId; Priority = priority; ReportedProblem = NormalizeOptionalText(reportedProblem);
        ReplaceItems(items, now); ReplaceChecklistItems(checklistItems); UpdatedAtUtc = now.ToUniversalTime();
    }

    public void UpdateAdditionalItems(IReadOnlyCollection<WorkOrderItemDefinition> additionalItems, DateTimeOffset now)
    {
        if (Status is WorkOrderStatus.Completed or WorkOrderStatus.Cancelled) throw new DomainException("Additional items cannot be updated for a closed work order.");
        if (additionalItems is null || additionalItems.Any(item => item.Type != WorkOrderItemType.Additional)) throw new DomainException("Only additional work order items can be updated.");

        _items.RemoveAll(item => item.Type == WorkOrderItemType.Additional);
        var utcNow = now.ToUniversalTime();
        var displayOrder = _items.Count;
        foreach (var item in additionalItems)
        {
            _items.Add(WorkOrderItem.Create(CompanyId, Id, item, displayOrder++, utcNow));
        }

        UpdatedAtUtc = utcNow;
    }

    public void AssignProfessional(Guid? professionalId, DateTimeOffset now)
    {
        EnsurePending(); ProfessionalId = professionalId; UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Start(DateTimeOffset now)
    {
        EnsurePending();
        if (ProfessionalId is null) throw new DomainException("A professional is required to start a work order.");
        Status = WorkOrderStatus.InProgress; StartedAtUtc = now.ToUniversalTime(); UpdatedAtUtc = StartedAtUtc.Value;
    }

    public void SaveChecklistProgress(IReadOnlyDictionary<Guid, bool> progress, DateTimeOffset now)
    {
        EnsureInProgress();
        if (progress is null) throw new DomainException("Checklist progress is required.");
        if (progress.Keys.Any(id => _checklistItems.All(item => item.Id != id))) throw new DomainException("Checklist item does not belong to this work order.");
        foreach (var item in _checklistItems.Where(item => progress.ContainsKey(item.Id))) item.SetCompletion(progress[item.Id], now);
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void UpdateExecutionNotes(string? executionNotes, DateTimeOffset now)
    {
        EnsureInProgress(); ExecutionNotes = string.IsNullOrWhiteSpace(executionNotes) ? null : executionNotes.Trim(); UpdatedAtUtc = now.ToUniversalTime();
    }

    public WorkOrderPhoto AddPhoto(WorkOrderPhotoType type, string fileKey, string? description, Guid uploadedByUserId, DateTimeOffset now)
    {
        EnsureInProgress();
        var photo = WorkOrderPhoto.Create(CompanyId, Id, type, fileKey, description, uploadedByUserId, now);
        _photos.Add(photo); UpdatedAtUtc = now.ToUniversalTime();
        return photo;
    }

    public WorkOrderPhoto RemovePhoto(Guid photoId, DateTimeOffset now)
    {
        EnsureInProgress();
        var photo = _photos.SingleOrDefault(item => item.Id == photoId) ?? throw new DomainException("Photo was not found.");
        _photos.Remove(photo); UpdatedAtUtc = now.ToUniversalTime(); return photo;
    }

    public void Complete(bool requireBeforePhotos, bool requireAfterPhotos, DateTimeOffset now)
    {
        EnsureInProgress();
        if (_checklistItems.Any(item => item.IsRequired && !item.IsCompleted)) throw new DomainException("Required checklist items must be completed.");
        if (requireBeforePhotos && _photos.All(photo => photo.Type != WorkOrderPhotoType.Before)) throw new DomainException("Before photos are required.");
        if (requireAfterPhotos && _photos.All(photo => photo.Type != WorkOrderPhotoType.After)) throw new DomainException("After photos are required.");
        Status = WorkOrderStatus.Completed; CompletedAtUtc = now.ToUniversalTime(); UpdatedAtUtc = CompletedAtUtc.Value;
    }

    public void Cancel(DateTimeOffset now)
    {
        if (Status == WorkOrderStatus.Completed) throw new DomainException("Completed work orders cannot be cancelled.");
        if (Status == WorkOrderStatus.Cancelled) throw new DomainException("Work order is already cancelled.");
        Status = WorkOrderStatus.Cancelled; UpdatedAtUtc = now.ToUniversalTime();
    }

    private void ReplaceItems(IReadOnlyCollection<WorkOrderItemDefinition> items, DateTimeOffset now)
    {
        if (items is null) throw new DomainException("Work order items are required.");
        var utcNow = now.ToUniversalTime(); var normalized = items.ToArray();
        if (normalized.Count(item => item.Type == WorkOrderItemType.Service) != 1 || normalized.Any(item => !Enum.IsDefined(item.Type))) throw new DomainException("Work order items must contain exactly one service item.");
        _items.Clear();
        for (var index = 0; index < normalized.Length; index++) _items.Add(WorkOrderItem.Create(CompanyId, Id, normalized[index], index, utcNow));
    }

    private void ReplaceChecklistItems(IReadOnlyCollection<WorkOrderChecklistItemDefinition> items)
    {
        if (items is null) throw new DomainException("Checklist items are required.");
        var normalized = items.OrderBy(item => item.DisplayOrder).ToArray();
        if (normalized.Any(item => item is null) || normalized.Select(item => item.DisplayOrder).Distinct().Count() != normalized.Length) throw new DomainException("Checklist display order must be unique.");
        _checklistItems.Clear(); foreach (var item in normalized) _checklistItems.Add(WorkOrderChecklistItem.Create(CompanyId, Id, item));
    }

    private void EnsurePending() { if (Status != WorkOrderStatus.Pending) throw new DomainException("Work order must be pending."); }
    private void EnsureInProgress() { if (Status != WorkOrderStatus.InProgress) throw new DomainException("Work order must be in progress."); }
    private static void RequireId(Guid value, string name) { if (value == Guid.Empty) throw new DomainException($"{name} ID is required."); }
    private static string? NormalizeOptionalText(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
