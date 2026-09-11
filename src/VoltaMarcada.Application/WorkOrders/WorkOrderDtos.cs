using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public sealed record WorkOrderListItem(Guid Id, string CustomerName, string EquipmentDescription, string ServiceName, WorkOrderStatus Status, WorkOrderPriority Priority, DateTimeOffset CreatedAtUtc, Guid? ProfessionalId, DateTimeOffset? ScheduledAtUtc = null);
public sealed record WorkOrderItemDto(Guid Id, string Description, decimal Quantity, decimal UnitPrice, WorkOrderItemType Type, decimal TotalAmount, int DisplayOrder);
public sealed record WorkOrderChecklistItemDto(Guid Id, string Description, bool IsRequired, bool IsCompleted, DateTimeOffset? CompletedAtUtc, int DisplayOrder);
public sealed record WorkOrderPhotoDto(Guid Id, WorkOrderPhotoType Type, string? Description, DateTimeOffset CreatedAtUtc);
public sealed record WorkOrderAttachmentDto(Guid Id, string OriginalFileName, string ContentType, long Length, string? Description, DateTimeOffset CreatedAtUtc);
public sealed record WorkOrderHistoryEntryDto(WorkOrderHistoryEventType EventType, string Description, string? ActorName, DateTimeOffset CreatedAtUtc);
public sealed record WorkOrderChargeInstallmentDto(Guid Id, PaymentMethod PaymentMethod, decimal Amount, string? OtherPaymentMethodDescription, WorkOrderChargeInstallmentStatus Status, DateTimeOffset? PaidAtUtc);
public sealed record WorkOrderCapabilities(bool CanEdit, bool CanAssignProfessional, bool CanStart, bool CanExecute, bool CanManageAdditionalItems, bool CanManagePayments = false);
public sealed record WorkOrderDetailDto(Guid Id, Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderStatus Status, WorkOrderPriority Priority, string? ReportedProblem, string? ExecutionNotes, bool RequireBeforePhotos, bool RequireAfterPhotos, decimal DiscountAmount, decimal TotalAmount, IReadOnlyList<WorkOrderItemDto> Items, IReadOnlyList<WorkOrderChecklistItemDto> ChecklistItems, IReadOnlyList<WorkOrderPhotoDto> Photos, IReadOnlyList<WorkOrderAttachmentDto> Attachments, IReadOnlyList<WorkOrderHistoryEntryDto> HistoryEntries, WorkOrderCapabilities Capabilities, bool PaymentControlEnabled = false, IReadOnlyList<WorkOrderChargeInstallmentDto>? ChargeInstallments = null, string? CustomerWhatsApp = null);
public sealed record WorkOrderOption(Guid Id, string Name);
public sealed record WorkOrderFormOptionsDto(IReadOnlyList<WorkOrderOption> Customers, IReadOnlyList<WorkOrderOption> Equipment, IReadOnlyList<WorkOrderOption> Services, IReadOnlyList<WorkOrderOption> Professionals);




