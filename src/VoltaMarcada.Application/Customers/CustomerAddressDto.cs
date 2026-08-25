using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Customers;

public sealed record CustomerAddressDto(
    Guid Id,
    CustomerAddressType AddressType,
    bool IsPrimary,
    string Street,
    string Number,
    string PostalCode,
    string? Complement,
    Guid StateId,
    string StateName,
    string StateAbbreviation,
    Guid CityId,
    string CityName);
