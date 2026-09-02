using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.MaintenanceOpportunities;

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
    int NextThirtyDaysCount,
    int AwaitingCustomerCount);

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
    MaintenanceOpportunityUrgency Urgency,
    MaintenanceOpportunityConversionType? ConversionType,
    Guid? ConvertedEntityId,
    DateTimeOffset? ConvertedAtUtc,
    DateTimeOffset? ClosedAtUtc,
    string? ClosureReason,
    MaintenanceOpportunityConversionReference? ConversionReference,
    IReadOnlyList<CustomerContactHistoryItem> Contacts);

public sealed record MaintenanceOpportunityConversionReference(
    Guid EntityId,
    DateTimeOffset? AppointmentStartAtUtc,
    QuoteStatus? QuoteStatus);

public sealed record MaintenanceOpportunityContactContext(
    Guid OpportunityId,
    Guid CustomerId,
    string CustomerName,
    string CustomerWhatsApp,
    string EquipmentDescription,
    string RecommendedServiceName,
    DateOnly DueDate,
    MaintenanceOpportunityStatus Status);

public sealed record CustomerContactHistoryItem(
    Guid Id,
    CustomerContactChannel Channel,
    CustomerContactStatus Status,
    string Message,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? SentAtUtc);

public sealed record PreparedWhatsAppContact(Guid ContactId, string Link);

public sealed record MaintenanceOpportunityScheduleContext(
    Guid OpportunityId,
    Guid CustomerId,
    Guid EquipmentId,
    Guid RecommendedServiceId);

public sealed record MaintenanceOpportunityQuoteContext(
    Guid OpportunityId,
    Guid CustomerId,
    Guid EquipmentId,
    Guid RecommendedServiceId);
