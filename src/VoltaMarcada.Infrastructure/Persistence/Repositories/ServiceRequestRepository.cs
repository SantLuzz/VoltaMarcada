using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class ServiceRequestRepository(AppDbContext dbContext) : IServiceRequestRepository
{
    public Task AddAsync(ServiceRequest request, CancellationToken cancellationToken = default) => dbContext.ServiceRequests.AddAsync(request, cancellationToken).AsTask();
    public Task<ServiceRequest?> GetForCompanyAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.ServiceRequests.SingleOrDefaultAsync(x => x.Id == requestId && x.CompanyId == companyId, cancellationToken);
    public Task<ServiceRequest?> GetForAppointmentRequestAsync(Guid appointmentRequestId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.ServiceRequests.SingleOrDefaultAsync(x => x.AppointmentRequestId == appointmentRequestId && x.CompanyId == companyId, cancellationToken);

    public async Task<IReadOnlyList<ServiceRequestListItem>> GetOpenForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await (
            from serviceRequest in dbContext.ServiceRequests.AsNoTracking()
            join appointmentRequest in dbContext.AppointmentRequests.AsNoTracking() on serviceRequest.AppointmentRequestId equals appointmentRequest.Id
            join customer in dbContext.Customers.AsNoTracking() on serviceRequest.CustomerId equals customer.Id
            join service in dbContext.Services.AsNoTracking() on appointmentRequest.ServiceId equals service.Id
            join professional in dbContext.Professionals.AsNoTracking().Where(item => item.CompanyId == companyId) on appointmentRequest.ProfessionalId equals professional.Id into professionals
            from professional in professionals.DefaultIfEmpty()
            join companyUser in dbContext.CompanyUsers.AsNoTracking().Where(item => item.CompanyId == companyId) on professional.CompanyUserId equals companyUser.Id into companyUsers
            from companyUser in companyUsers.DefaultIfEmpty()
            join user in dbContext.Users.AsNoTracking() on companyUser.UserId equals user.Id into users
            from user in users.DefaultIfEmpty()
            where serviceRequest.CompanyId == companyId &&
                  appointmentRequest.CompanyId == companyId &&
                  customer.CompanyId == companyId &&
                  service.CompanyId == companyId &&
                  serviceRequest.Status == ServiceRequestStatus.Pending &&
                  appointmentRequest.Status == AppointmentRequestStatus.Pending
            orderby appointmentRequest.RequestedStartAtUtc, serviceRequest.CreatedAtUtc
            select new ServiceRequestListItem(
                serviceRequest,
                appointmentRequest,
                customer.Name,
                customer.WhatsApp,
                service.Name,
                user == null ? string.Empty : user.FullName ?? user.Email ?? string.Empty)
        ).ToArrayAsync(cancellationToken);
    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
