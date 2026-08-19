using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Customers;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Repositories;

public sealed class CustomerRepository(AppDbContext dbContext) : ICustomerRepository
{
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

    public async Task<IReadOnlyList<Customer>> GetAllForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await IncludeAddressesAndLocations()
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

    public void RemoveAddresses(Customer customer) =>
        dbContext.CustomerAddresses.RemoveRange(customer.Addresses);

    public void Remove(Customer customer) =>
        dbContext.Customers.Remove(customer);

    public async Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        await dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<Customer> IncludeAddressesAndLocations() =>
        dbContext.Customers
            .Include(customer => customer.Addresses)
            .ThenInclude(address => address.State)
            .Include(customer => customer.Addresses)
            .ThenInclude(address => address.City);
}
