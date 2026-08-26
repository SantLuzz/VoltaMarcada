using Microsoft.EntityFrameworkCore;
using Npgsql;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Customers;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
    private const string DuplicateDocumentConstraintName = "UX_Customers_CompanyId_Document";

    public async Task<bool> AreLocationsValidAsync(
        IReadOnlyCollection<CustomerAddressInput> addresses,
        CancellationToken cancellationToken = default)
    {
        var locations = addresses
            .Select(address => (address.CityId, address.StateId))
            .Distinct()
            .ToArray();

        var cityIds = locations.Select(location => location.CityId).ToArray();
        var cities = await dbContext.Cities
            .Where(city => cityIds.Contains(city.Id))
            .Select(city => new { city.Id, city.StateId })
            .ToListAsync(cancellationToken);

        return locations.All(location => cities.Any(city =>
            city.Id == location.CityId && city.StateId == location.StateId));
    }

    public async Task AddAsync(Customer customer, CancellationToken cancellationToken = default) =>
        await dbContext.Customers.AddAsync(customer, cancellationToken);

    public Task<bool> DocumentExistsAsync(
        Guid companyId,
        string normalizedDocument,
        Guid? excludedCustomerId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.Customers.AnyAsync(customer =>
            customer.CompanyId == companyId &&
            customer.Document == normalizedDocument &&
            (!excludedCustomerId.HasValue || customer.Id != excludedCustomerId.Value),
            cancellationToken);

    public async Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(
        Guid companyId,
        GetCustomersQuery query,
        CancellationToken cancellationToken = default) =>
        await ApplyFilters(IncludeAddressesAndLocations(), query)
            .AsNoTracking()
            .Where(customer => customer.CompanyId == companyId)
            .OrderBy(customer => customer.Name)
            .ToListAsync(cancellationToken);

    public async Task<Customer?> GetForCompanyAsync(
        Guid customerId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await IncludeAddressesAndLocations()
            .SingleOrDefaultAsync(
                customer => customer.Id == customerId && customer.CompanyId == companyId,
                cancellationToken);

    public Task<Customer?> GetByDocumentAsync(Guid companyId, string normalizedDocument, CancellationToken cancellationToken = default) =>
        IncludeAddressesAndLocations().SingleOrDefaultAsync(
            customer => customer.CompanyId == companyId && customer.Document == normalizedDocument,
            cancellationToken);

    public void RemoveAddresses(Customer customer) =>
        dbContext.CustomerAddresses.RemoveRange(customer.Addresses);

    public void RemoveAddress(CustomerAddress address) => dbContext.CustomerAddresses.Remove(address);

    public void Remove(Customer customer) =>
        dbContext.Customers.Remove(customer);

    public async Task<CustomerPersistenceOutcome> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return CustomerPersistenceOutcome.Success;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException &&
                                                postgresException.ConstraintName == DuplicateDocumentConstraintName)
        {
            return CustomerPersistenceOutcome.DuplicateDocument;
        }
    }

    private IQueryable<Customer> IncludeAddressesAndLocations() =>
        dbContext.Customers
            .Include(customer => customer.Addresses)
            .ThenInclude(address => address.State)
            .Include(customer => customer.Addresses)
            .ThenInclude(address => address.City);

    private static IQueryable<Customer> ApplyFilters(
        IQueryable<Customer> customers,
        GetCustomersQuery query)
    {
        if (query.SearchFilter is not { } searchFilter)
        {
            return customers;
        }

        var documentSearchFilter = query.DocumentSearchFilter;
        return documentSearchFilter.Length == 0
            ? customers.Where(customer => EF.Functions.ILike(
                customer.Name,
                ToContainsPattern(searchFilter),
                "\\"))
            : customers.Where(customer =>
                EF.Functions.ILike(customer.Name, ToContainsPattern(searchFilter), "\\") ||
                customer.Document.Contains(documentSearchFilter));
    }

    private static string ToContainsPattern(string value) =>
        $"%{value.Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
}
