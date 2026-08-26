using VoltaMarcada.Application.Customers;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface ICustomerRepository
{
    Task<bool> AreLocationsValidAsync(
        IReadOnlyCollection<CustomerAddressInput> addresses,
        CancellationToken cancellationToken = default);

    Task AddAsync(Customer customer, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(
        Guid companyId,
        string normalizedDocument,
        Guid? excludedCustomerId = null,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(
        Guid companyId,
        GetCustomersQuery query,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetForCompanyAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<Customer?> GetByDocumentAsync(Guid companyId, string normalizedDocument, CancellationToken cancellationToken = default);

    void RemoveAddresses(Customer customer);
    void RemoveAddress(CustomerAddress address);

    void Remove(Customer customer);

    Task<CustomerPersistenceOutcome> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public enum CustomerPersistenceOutcome
{
    Success,
    DuplicateDocument
}
