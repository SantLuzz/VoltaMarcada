using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.ServiceRequests;

public sealed class GetServiceRequestsUseCase(WorkOrderAuthorization authorization, IServiceRequestRepository repository)
{
    public async Task<IReadOnlyList<ServiceRequestDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var requests = await repository.GetOpenForCompanyAsync(scope.CompanyId, cancellationToken);
        return requests
            .Where(item => scope.IsOwner || item.AppointmentRequest.ProfessionalId == scope.ProfessionalId)
            .Select(item => new ServiceRequestDto(
                item.AppointmentRequest.Id,
                item.ServiceRequest.CustomerId,
                item.CustomerName,
                item.CustomerWhatsApp,
                item.AppointmentRequest.ServiceId,
                item.ServiceName,
                item.AppointmentRequest.ProfessionalId,
                item.ProfessionalName,
                item.AppointmentRequest.RequestedDate,
                item.AppointmentRequest.RequestedStartTime,
                item.AppointmentRequest.DurationMinutes,
                item.ServiceRequest.ProblemDescription,
                item.AppointmentRequest.ProposedCustomerName is not null,
                item.ServiceRequest.Status))
            .ToArray();
    }
}
