using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface ICustomerContactRepository
{
    Task AddAsync(CustomerContact contact, CancellationToken cancellationToken = default);

    Task<CustomerContact?> GetForCompanyAsync(
        Guid contactId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
