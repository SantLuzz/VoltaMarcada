using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class ServiceRequestRepository(AppDbContext dbContext) : IServiceRequestRepository
{
    public Task AddAsync(ServiceRequest request, CancellationToken cancellationToken = default) => dbContext.ServiceRequests.AddAsync(request, cancellationToken).AsTask();
    public Task<ServiceRequest?> GetForAppointmentRequestAsync(Guid appointmentRequestId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.ServiceRequests.SingleOrDefaultAsync(x => x.AppointmentRequestId == appointmentRequestId && x.CompanyId == companyId, cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
