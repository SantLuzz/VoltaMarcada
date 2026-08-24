using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderItemInput(string Description, decimal Quantity, decimal UnitPrice);
public sealed record WorkOrderAppointmentInput(DateOnly Date, TimeOnly StartTime);
public sealed record CreateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderPriority Priority, string? ReportedProblem, decimal? ServiceUnitPrice, IReadOnlyList<WorkOrderItemInput> AdditionalItems, WorkOrderAppointmentInput? Appointment = null);
public sealed record UpdateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderPriority Priority, string? ReportedProblem, decimal? ServiceUnitPrice, IReadOnlyList<WorkOrderItemInput> AdditionalItems);
public sealed record ChecklistProgressInput(Guid ChecklistItemId, bool IsCompleted);
public sealed record SaveWorkOrderChecklistProgressCommand(IReadOnlyList<ChecklistProgressInput> ChecklistItems, string? ExecutionNotes);
public sealed record MaintenanceRecommendationInput(int? IntervalDays, DateOnly? DueDate, Guid RecommendedServiceId, string? Notes);
public sealed record CompleteWorkOrderCommand(MaintenanceRecommendationInput? MaintenanceRecommendation = null);
public sealed record GetWorkOrdersQuery(string? SearchFilter, WorkOrderStatus? Status);
