using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface ICustomerContactRepository
{
    Task AddAsync(CustomerContact contact, CancellationToken cancellationToken = default);

    Task<CustomerContact?> GetForCompanyAsync(
        Guid contactId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
