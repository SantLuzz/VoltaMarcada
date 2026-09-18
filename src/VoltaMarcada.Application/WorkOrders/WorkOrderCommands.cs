using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.WorkOrders;

public record WorkOrderItemInput(Guid? EquipmentId, Guid? ServiceId, string Description, decimal Quantity, decimal UnitPrice, WorkOrderItemType Type)
{
    public WorkOrderItemInput(string description, decimal quantity, decimal unitPrice)
        : this(null, null, description, quantity, unitPrice, WorkOrderItemType.Additional)
    {
    }
}

public sealed record WorkOrderServiceItemInput : WorkOrderItemInput
{
    public WorkOrderServiceItemInput(Guid equipmentId, Guid serviceId, string description, decimal quantity, decimal unitPrice)
        : base(equipmentId, serviceId, description, quantity, unitPrice, WorkOrderItemType.Service) { }
}
public sealed record WorkOrderAppointmentInput(DateOnly Date, TimeOnly StartTime);
public sealed record CreateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderPriority Priority, string? ReportedProblem, decimal? ServiceUnitPrice, decimal DiscountAmount, IReadOnlyList<WorkOrderItemInput> AdditionalItems, WorkOrderAppointmentInput? Appointment = null, Guid? SourceAppointmentId = null, IReadOnlyList<WorkOrderItemInput>? Items = null);
public sealed record UpdateWorkOrderCommand(Guid CustomerId, Guid EquipmentId, Guid ServiceId, Guid? ProfessionalId, WorkOrderPriority Priority, string? ReportedProblem, decimal? ServiceUnitPrice, decimal DiscountAmount, IReadOnlyList<WorkOrderItemInput> AdditionalItems, IReadOnlyList<WorkOrderItemInput>? Items = null);
public sealed record UpdateWorkOrderAdditionalItemsCommand(IReadOnlyList<WorkOrderItemInput> AdditionalItems);
public sealed record ChecklistProgressInput(Guid ChecklistItemId, bool IsCompleted);
public sealed record SaveWorkOrderChecklistProgressCommand(IReadOnlyList<ChecklistProgressInput> ChecklistItems, string? ExecutionNotes);
public sealed record MaintenanceRecommendationInput(int? IntervalDays, DateOnly? DueDate, Guid RecommendedServiceId, string? Notes);
public sealed record MaintenanceRecommendationItemInput(Guid EquipmentId, Guid RecommendedServiceId);
public sealed record CompleteWorkOrderCommand(MaintenanceRecommendationInput? MaintenanceRecommendation = null, WorkOrderPaymentDecisionInput? PaymentDecision = null, IReadOnlyList<MaintenanceRecommendationItemInput>? MaintenanceRecommendationItems = null);
public sealed record GetWorkOrdersQuery(string? SearchFilter, WorkOrderStatus? Status);
