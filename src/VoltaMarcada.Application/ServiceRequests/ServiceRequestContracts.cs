using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.ServiceRequests;

public enum ServiceRequestResolution
{
    PrepareQuote,
    ScheduleVisit
}

public sealed record ServiceRequestDto(
    Guid AppointmentRequestId,
    Guid CustomerId,
    string CustomerName,
    string CustomerWhatsApp,
    Guid ServiceId,
    string ServiceName,
    Guid? ProfessionalId,
    string ProfessionalName,
    DateOnly RequestedDate,
    TimeOnly RequestedStartTime,
    int DurationMinutes,
    string? ProblemDescription,
    bool HasCustomerProfileProposal,
    ServiceRequestStatus Status);

public sealed record ResolveServiceRequestCommand(
    Guid AppointmentRequestId,
    ServiceRequestResolution Resolution,
    Guid EquipmentId,
    Guid? ProfessionalId,
    string? AppointmentDescription,
    decimal? ServiceUnitPrice,
    bool ApplyCustomerProfileProposal);

public sealed record ServiceRequestResolutionResult(Guid? AppointmentId, Guid? QuoteId);
