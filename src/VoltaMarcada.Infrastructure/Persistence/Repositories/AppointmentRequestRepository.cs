using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Identity;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRequestRepository(AppDbContext dbContext) : IAppointmentRequestRepository
{
    public Task AddAsync(AppointmentRequest request, CancellationToken cancellationToken = default) =>
        dbContext.AppointmentRequests.AddAsync(request, cancellationToken).AsTask();

    public Task<AppointmentRequest?> GetForCompanyAsync(Guid requestId, Guid companyId, CancellationToken cancellationToken = default) =>
        dbContext.AppointmentRequests.SingleOrDefaultAsync(
            request => request.Id == requestId && request.CompanyId == companyId,
            cancellationToken);

    public Task<AppointmentRequest?> GetForResponseTokenAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        dbContext.AppointmentRequests.SingleOrDefaultAsync(
            request => request.ResponseTokenHash == tokenHash && request.ResponseTokenRevokedAtUtc == null,
            cancellationToken);

    public Task<AppointmentRequestResponseContext?> GetResponseContextAsync(string tokenHash, CancellationToken cancellationToken = default) =>
        (
            from request in dbContext.AppointmentRequests
            join company in dbContext.Companies.AsNoTracking() on request.CompanyId equals company.Id
            join customer in dbContext.Customers.AsNoTracking() on new { request.CompanyId, request.CustomerId } equals new { customer.CompanyId, CustomerId = customer.Id }
            join service in dbContext.Services.AsNoTracking() on new { request.CompanyId, request.ServiceId } equals new { service.CompanyId, ServiceId = service.Id }
            where request.ResponseTokenHash == tokenHash && request.ResponseTokenRevokedAtUtc == null
            select new AppointmentRequestResponseContext(
                request,
                company.Slug,
                company.TradeName,
                customer.Name,
                service.Name,
                string.Empty)
        ).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<AppointmentRequestListItem>> GetOpenForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await (
            from request in dbContext.AppointmentRequests.AsNoTracking()
            join customer in dbContext.Customers.AsNoTracking().Where(item => item.CompanyId == companyId) on request.CustomerId equals customer.Id
            join service in dbContext.Services.AsNoTracking().Where(item => item.CompanyId == companyId) on request.ServiceId equals service.Id
            where request.CompanyId == companyId &&
                  (request.Status == AppointmentRequestStatus.Pending || request.Status == AppointmentRequestStatus.RescheduleRequested)
            orderby request.RequestedDate, request.RequestedStartTime
            select new AppointmentRequestListItem(
                request,
                customer.Name,
                customer.WhatsApp,
                service.Name,
                string.Empty,
                null)
        ).ToArrayAsync(cancellationToken);

    public Task<bool> HasOpenForMaintenanceOpportunityAsync(Guid companyId, Guid opportunityId, CancellationToken cancellationToken = default) =>
        dbContext.AppointmentRequests.AsNoTracking().AnyAsync(
            request => request.CompanyId == companyId &&
                       request.MaintenanceOpportunityId == opportunityId &&
                       (request.Status == AppointmentRequestStatus.Pending || request.Status == AppointmentRequestStatus.RescheduleRequested),
            cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
