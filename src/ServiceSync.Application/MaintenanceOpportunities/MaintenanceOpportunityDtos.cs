using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.MaintenanceOpportunities;

public enum MaintenanceOpportunityUrgency
{
    Overdue,
    NextSevenDays,
    NextThirtyDays,
    Future
}

public sealed record GetMaintenanceOpportunitiesQuery(
    MaintenanceOpportunityStatus? Status,
    MaintenanceOpportunityUrgency? Urgency);

public sealed record MaintenanceOpportunitySummary(
    int OverdueCount,
    int NextSevenDaysCount,
    int NextThirtyDaysCount);

public sealed record MaintenanceOpportunityListItem(
    Guid Id,
    string CustomerName,
    string CustomerWhatsApp,
    string EquipmentDescription,
    DateTimeOffset PreviousWorkOrderCompletedAtUtc,
    string RecommendedServiceName,
    DateOnly DueDate,
    string Notes,
    MaintenanceOpportunityStatus Status,
    MaintenanceOpportunityUrgency Urgency);

public sealed record MaintenanceOpportunityScheduleContext(
    Guid OpportunityId,
    Guid CustomerId,
    Guid EquipmentId,
    Guid RecommendedServiceId);
