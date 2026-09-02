using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.Scheduling;

public sealed record AppointmentRequestDto(
    Guid Id,
    string CustomerName,
    string CustomerWhatsApp,
    Guid ServiceId,
    string ServiceName,
    Guid? ProfessionalId,
    string ProfessionalName,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationMinutes,
    VoltaMarcada.Domain.Enums.AppointmentRequestStatus Status,
    bool RequiresCommercialResolution,
    bool HasCustomerProfileProposal,
    string? ProposedCustomerName,
    string? ProposedCustomerWhatsApp,
    string? ProposedCustomerEmail,
    string? ProposedAddress,
    IReadOnlyList<AvailableProfessionalDto> RequestedTimeProfessionals,
    IReadOnlyList<AppointmentAlternativeDto> Alternatives,
    string? EquipmentDescription = null);

public sealed class GetAppointmentRequestsUseCase(
    WorkOrderAuthorization authorization,
    IAppointmentRequestRepository repository,
    IServiceRequestRepository serviceRequests,
    AnalyzeAppointmentRequestAvailabilityUseCase availabilityAnalysis)
{
    public async Task<IReadOnlyList<AppointmentRequestDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner)
        {
            return [];
        }

        var requests = await repository.GetOpenForCompanyAsync(scope.CompanyId, cancellationToken);
        var result = new List<AppointmentRequestDto>();
        foreach (var item in requests)
        {
            var analysis = item.Request.Status == VoltaMarcada.Domain.Enums.AppointmentRequestStatus.Pending
                ? await availabilityAnalysis.ExecuteAsync(item.Request.Id, scope.CompanyId, cancellationToken)
                : null;
            var serviceRequest = await serviceRequests.GetForAppointmentRequestAsync(item.Request.Id, scope.CompanyId, cancellationToken);
            result.Add(new AppointmentRequestDto(
                item.Request.Id,
                item.CustomerName,
                item.CustomerWhatsApp,
                item.Request.ServiceId,
                item.ServiceName,
                item.Request.ProposedProfessionalId ?? item.Request.ProfessionalId,
                item.ProfessionalName,
                item.Request.ProposedDate ?? item.Request.RequestedDate,
                item.Request.ProposedStartTime ?? item.Request.RequestedStartTime,
                item.Request.ProposedDurationMinutes ?? item.Request.DurationMinutes,
                item.Request.Status,
                serviceRequest?.Status == VoltaMarcada.Domain.Enums.ServiceRequestStatus.Pending,
                item.Request.ProposedCustomerName is not null,
                item.Request.ProposedCustomerName,
                item.Request.ProposedCustomerWhatsApp,
                item.Request.ProposedCustomerEmail,
                item.Request.ProposedAddressStreet is null
                    ? null
                    : $"{item.Request.ProposedAddressStreet}, {item.Request.ProposedAddressNumber} - CEP {item.Request.ProposedAddressPostalCode}",
                analysis?.RequestedTimeProfessionals ?? [],
                analysis?.Alternatives ?? [],
                item.EquipmentName));
        }

        return result;
    }
}
