using VoltaMarcada.Application.Services;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface IServiceRepository
{
    Task AddAsync(global::VoltaMarcada.Domain.Entities.Service service, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<global::VoltaMarcada.Domain.Entities.Service>> GetAllForCompanyAsync(
        Guid companyId,
        GetServicesQuery query,
        CancellationToken cancellationToken = default);

    Task<global::VoltaMarcada.Domain.Entities.Service?> GetForCompanyAsync(
        Guid serviceId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    void Remove(global::VoltaMarcada.Domain.Entities.Service service);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
