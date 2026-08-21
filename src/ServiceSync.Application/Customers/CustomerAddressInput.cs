using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Customers;

public sealed record CustomerAddressInput(
    CustomerAddressType AddressType,
    bool IsPrimary,
    string Street,
    string Number,
    string PostalCode,
    string? Complement,
    Guid StateId,
    Guid CityId);
