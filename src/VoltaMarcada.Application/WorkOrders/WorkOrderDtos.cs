using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed record WorkOrderListItem(Guid Id, string CustomerName, string EquipmentDescription, string ServiceName, WorkOrderStatus Status, WorkOrderPriority Priority, DateTimeOffset CreatedAtUtc, Guid? ProfessionalId);
public sealed record WorkOrderItemDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, WorkOrderItemType Type, decimal TotalAmount, int DisplayOrder);
public sealed record WorkOrderChecklistItemDto(Guid Id, string Description, bool IsRequired, bool IsCompleted, DateTimeOffset? CompletedAtUtc, int DisplayOrder);
public sealed record WorkOrderPhotoDto(Guid Id, WorkOrderPhotoType Type, string? Description, DateTimeOffset CreatedAtUtc);
public sealed record WorkOrderCapabilities(bool CanEdit, bool CanAssignProfessional, bool CanStart, bool CanExecute, bool CanManageAdditionalItems);
public sealed record WorkOrderDetailDto(Guid Id, Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderStatus Status, WorkOrderPriority Priority, string? ReportedProblem, string? ExecutionNotes, bool RequireBeforePhotos, bool RequireAfterPhotos, decimal TotalAmount, IReadOnlyList<WorkOrderItemDto> Items, IReadOnlyList<WorkOrderChecklistItemDto> ChecklistItems, IReadOnlyList<WorkOrderPhotoDto> Photos, WorkOrderCapabilities Capabilities);
public sealed record WorkOrderOption(Guid Id, string Name);
public sealed record WorkOrderFormOptionsDto(IReadOnlyList<WorkOrderOption> Customers, IReadOnlyList<WorkOrderOption> Equipment, IReadOnlyList<WorkOrderOption> Services, IReadOnlyList<WorkOrderOption> Professionals);
