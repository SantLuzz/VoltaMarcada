using ServiceSync.Domain.Enums;
using ServiceSync.Domain.Exceptions;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Domain.Entities;

public sealed class CustomerAddress
{
    private CustomerAddress()
    {
    }

    public Guid Id { get; private set; }

    public Guid CustomerId { get; private set; }

    public CustomerAddressType AddressType { get; private set; }

    public bool IsPrimary { get; private set; }

    public string Street { get; private set; } = string.Empty;

    public string Number { get; private set; } = string.Empty;

    public string PostalCode { get; private set; } = string.Empty;

    public string? Complement { get; private set; }

    public Guid StateId { get; private set; }

    public State State { get; private set; } = null!;

    public Guid CityId { get; private set; }

    public City City { get; private set; } = null!;

    public static CustomerAddress Create(
        CustomerAddressType addressType,
        bool isPrimary,
        string street,
        string number,
        string postalCode,
        string? complement,
        Guid stateId,
        Guid cityId)
    {
        if (!Enum.IsDefined(addressType))
        {
            throw new DomainException("Customer address type is invalid.");
        }

        if (!BrazilianPostalCode.TryNormalize(postalCode, out var normalizedPostalCode))
        {
            throw new DomainException("A valid Brazilian postal code is required.");
        }

        if (stateId == Guid.Empty)
        {
            throw new DomainException("State ID is required.");
        }

        if (cityId == Guid.Empty)
        {
            throw new DomainException("City ID is required.");
        }

        return new CustomerAddress
        {
            Id = Guid.NewGuid(),
            AddressType = addressType,
            IsPrimary = isPrimary,
            Street = RequireText(street, "Street"),
            Number = RequireText(number, "Number"),
            PostalCode = normalizedPostalCode,
            Complement = NormalizeOptionalText(complement),
            StateId = stateId,
            CityId = cityId
        };
    }

    private static string RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
