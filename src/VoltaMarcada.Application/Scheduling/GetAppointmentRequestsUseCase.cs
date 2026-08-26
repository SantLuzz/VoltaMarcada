using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.Scheduling;

public sealed record AppointmentRequestDto(
    Guid Id,
    string CustomerName,
    string CustomerWhatsApp,
    Guid ServiceId,
    string ServiceName,
    Guid ProfessionalId,
    string ProfessionalName,
    DateOnly Date,
    TimeOnly StartTime,
    int DurationMinutes,
    VoltaMarcada.Domain.Enums.AppointmentRequestStatus Status,
    bool HasCustomerProfileProposal,
    string? ProposedCustomerName,
    string? ProposedCustomerWhatsApp,
    string? ProposedCustomerEmail,
    string? ProposedAddress);

public sealed class GetAppointmentRequestsUseCase(WorkOrderAuthorization authorization, IAppointmentRequestRepository repository)
{
    public async Task<IReadOnlyList<AppointmentRequestDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var requests = await repository.GetOpenForCompanyAsync(scope.CompanyId, cancellationToken);
        return requests
            .Where(item => scope.IsOwner || item.Request.ProfessionalId == scope.ProfessionalId)
            .Select(item => new AppointmentRequestDto(
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
                item.Request.ProposedCustomerName is not null,
                item.Request.ProposedCustomerName,
                item.Request.ProposedCustomerWhatsApp,
                item.Request.ProposedCustomerEmail,
                item.Request.ProposedAddressStreet is null
                    ? null
                    : $"{item.Request.ProposedAddressStreet}, {item.Request.ProposedAddressNumber} - CEP {item.Request.ProposedAddressPostalCode}"))
            .ToArray();
    }
}
