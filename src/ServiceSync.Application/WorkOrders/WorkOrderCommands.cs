using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderItemInput(string Description, decimal Quantity, decimal UnitPrice);
public sealed record CreateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderPriority Priority, string ReportedProblem, IReadOnlyList<WorkOrderItemInput> Items);
public sealed record UpdateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderPriority Priority, string ReportedProblem, IReadOnlyList<WorkOrderItemInput> Items);
public sealed record ChecklistProgressInput(Guid ChecklistItemId, bool IsCompleted);
public sealed record SaveWorkOrderChecklistProgressCommand(IReadOnlyList<ChecklistProgressInput> ChecklistItems, string? ExecutionNotes);
public sealed record GetWorkOrdersQuery(string? SearchFilter, WorkOrderStatus? Status);
