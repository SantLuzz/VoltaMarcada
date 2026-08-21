using ServiceSync.Application.Services;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IServiceRepository
{
    Task AddAsync(global::ServiceSync.Domain.Entities.Service service, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<global::ServiceSync.Domain.Entities.Service>> GetAllForCompanyAsync(
        Guid companyId,
        GetServicesQuery query,
        CancellationToken cancellationToken = default);

    Task<global::ServiceSync.Domain.Entities.Service?> GetForCompanyAsync(
        Guid serviceId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    void Remove(global::ServiceSync.Domain.Entities.Service service);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
