using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.ServiceRequests;

public enum ServiceRequestResolution
{
    PrepareQuote,
    CreateWorkOrder
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
    Guid ProfessionalId,
    ServiceRequestResolution Resolution,
    Guid EquipmentId,
    string AppointmentDescription,
    decimal? ServiceUnitPrice,
    bool ApplyCustomerProfileProposal);

public sealed record ServiceRequestResolutionResult(Guid AppointmentId, Guid? QuoteId, Guid? WorkOrderId);
