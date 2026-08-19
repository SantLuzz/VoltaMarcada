using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Customers;

public sealed record CustomerDto(
    Guid Id,
    string Name,
    string Document,
    string WhatsApp,
    string? Email,
    string? Notes,
    IReadOnlyList<CustomerAddressDto> Addresses)
{
    internal static CustomerDto FromEntity(Customer customer) =>
        new(
            customer.Id,
            customer.Name,
            customer.Document,
            customer.WhatsApp,
            customer.Email,
            customer.Notes,
            customer.Addresses
                .Select(address => new CustomerAddressDto(
                    address.Id,
                    address.AddressType,
                    address.IsPrimary,
                    address.Street,
                    address.Number,
                    address.PostalCode,
                    address.Complement,
                    address.StateId,
                    address.State.Name,
                    address.State.Abbreviation,
                    address.CityId,
                    address.City.Name))
                .ToArray());
}
