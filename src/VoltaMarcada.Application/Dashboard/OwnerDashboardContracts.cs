namespace VoltaMarcada.Application.Dashboard;

public sealed record OwnerDashboardDto(
    OwnerDashboardActionCounts Actions,
    OwnerMaintenanceValueFunnel ValueFunnel);

public sealed record OwnerDashboardActionCounts(
    int TodayAppointmentCount,
    int OverdueOpportunityCount,
    int DueSoonOpportunityCount,
    int PendingAppointmentRequestCount,
    int QuotesAwaitingResponseCount,
    int OpenWorkOrderCount);

public sealed record OwnerMaintenanceValueFunnel(
    int IdentifiedOpportunityCount,
    int ContactedCustomerCount,
    int MaintenanceRequestCount,
    int RecoveredBookingCount,
    int CompletedServiceCount,
    decimal RecoveredRevenue,
    int OpenOpportunityCount,
    decimal EstimatedPotentialRevenue);

public sealed record OwnerDashboardPeriod(
    DateTimeOffset TodayStartAtUtc,
    DateTimeOffset TomorrowStartAtUtc,
    DateTimeOffset MonthStartAtUtc,
    DateTimeOffset NextMonthStartAtUtc,
    DateOnly BusinessDate);
