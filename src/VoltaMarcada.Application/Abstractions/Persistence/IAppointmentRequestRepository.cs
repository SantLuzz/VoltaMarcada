using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IAppointmentRequestRepository
{
    Task AddAsync(AppointmentRequest request, CancellationToken cancellationToken = default);
    Task<AppointmentRequest?> GetForCompanyAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken = default);
    Task<AppointmentRequest?> GetForAppointmentAsync(Guid appointmentId, Guid companyId, CancellationToken cancellationToken = default);
    Task<AppointmentRequest?> GetForResponseTokenAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<AppointmentRequestResponseContext?> GetResponseContextAsync(string tokenHash, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentRequestListItem>> GetOpenForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenForMaintenanceOpportunityAsync(Guid companyId, Guid opportunityId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record AppointmentRequestListItem(
    AppointmentRequest Request,
    string CustomerName,
    string CustomerWhatsApp,
    string ServiceName,
    string ProfessionalName,
    string? EquipmentName);

public sealed record AppointmentRequestResponseContext(
    AppointmentRequest Request,
    string CompanySlug,
    string CompanyTradeName,
    string CustomerName,
    string ServiceName,
    string ProfessionalName);
