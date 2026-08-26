using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IServiceRequestRepository
{
    Task AddAsync(ServiceRequest request, CancellationToken cancellationToken = default);
    Task<ServiceRequest?> GetForCompanyAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken = default);
    Task<ServiceRequest?> GetForAppointmentRequestAsync(Guid appointmentRequestId, Guid companyId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ServiceRequestListItem>> GetOpenForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record ServiceRequestListItem(
    ServiceRequest ServiceRequest,
    AppointmentRequest AppointmentRequest,
    string CustomerName,
    string CustomerWhatsApp,
    string ServiceName,
    string ProfessionalName);
