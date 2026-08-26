using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Services;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class ServiceRepository(AppDbContext dbContext) : IServiceRepository
{
    public async Task<IReadOnlyList<global::VoltaMarcada.Domain.Entities.Service>> GetPublicBookingServicesAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        await dbContext.Services.AsNoTracking()
            .Where(service => service.CompanyId == companyId && service.IsActive && service.AllowOnlineBooking)
            .OrderBy(service => service.Name)
            .ToArrayAsync(cancellationToken);
    public async Task AddAsync(global::VoltaMarcada.Domain.Entities.Service service, CancellationToken cancellationToken = default) =>
        await dbContext.Services.AddAsync(service, cancellationToken);

    public async Task<IReadOnlyList<global::VoltaMarcada.Domain.Entities.Service>> GetAllForCompanyAsync(
        Guid companyId,
        GetServicesQuery query,
        CancellationToken cancellationToken = default)
    {
        var services = dbContext.Services
            .AsNoTracking()
            .Include(service => service.ChecklistItems)
            .Where(service => service.CompanyId == companyId);

        if (query.SearchFilter is { } searchFilter)
        {
            var pattern = ToContainsPattern(searchFilter);
            services = services.Where(service =>
                EF.Functions.ILike(service.Name, pattern, "\\") ||
                (service.Description != null && EF.Functions.ILike(service.Description, pattern, "\\")));
        }

        return await services
            .OrderBy(service => service.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<global::VoltaMarcada.Domain.Entities.Service?> GetForCompanyAsync(
        Guid serviceId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Services
            .Include(service => service.ChecklistItems)
            .SingleOrDefaultAsync(
                service => service.Id == serviceId && service.CompanyId == companyId,
                cancellationToken);

    public void Remove(global::VoltaMarcada.Domain.Entities.Service service) =>
        dbContext.Services.Remove(service);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);

    private static string ToContainsPattern(string value) =>
        $"%{value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
