using ServiceSync.Application.Customers;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<bool> AreLocationsValidAsync(
        IReadOnlyCollection<CustomerAddressInput> addresses,
        CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetForCompanyAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    void RemoveAddresses(Customer customer);

    void Remove(Customer customer);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
