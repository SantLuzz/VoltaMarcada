using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Exceptions;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Domain.Entities;

public sealed class Customer : ICompanyScopedEntity
{
    private readonly List<CustomerAddress> _addresses = [];

    private Customer()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string WhatsApp { get; private set; } = string.Empty;

    public string? Email { get; private set; }

    public string? Notes { get; private set; }

    public IReadOnlyCollection<CustomerAddress> Addresses => _addresses;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Customer Create(
        Guid companyId,
        string name,
        string document,
        string whatsApp,
        string? email,
        string? notes,
        IEnumerable<CustomerAddress> addresses,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        var normalizedAddresses = ValidateAddresses(addresses);

        if (!BrazilianDocument.TryNormalize(document, out var normalizedDocument))
        {
            throw new DomainException("A valid Brazilian document is required.");
        }

        var utcNow = now.ToUniversalTime();
        var customer = new Customer
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = RequireText(name, "Name"),
            Document = normalizedDocument,
            WhatsApp = RequireText(whatsApp, "WhatsApp"),
            Email = NormalizeOptionalText(email),
            Notes = NormalizeOptionalText(notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        customer.SetAddresses(normalizedAddresses);
        return customer;
    }

    public void Update(
        string name,
        string document,
        string whatsApp,
        string? email,
        string? notes,
        DateTimeOffset now)
    {
        if (!BrazilianDocument.TryNormalize(document, out var normalizedDocument))
        {
            throw new DomainException("A valid Brazilian document is required.");
        }

        Name = RequireText(name, "Name");
        Document = normalizedDocument;
        WhatsApp = RequireText(whatsApp, "WhatsApp");
        Email = NormalizeOptionalText(email);
        Notes = NormalizeOptionalText(notes);
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void ReplaceAddresses(IEnumerable<CustomerAddress> addresses)
    {
        var normalizedAddresses = ValidateAddresses(addresses);

        SetAddresses(normalizedAddresses);
    }

    private void SetAddresses(IEnumerable<CustomerAddress> addresses)
    {
        foreach (var address in addresses)
        {
            address.AssignToCustomer(Id);
        }

        _addresses.Clear();
        _addresses.AddRange(addresses);
    }

    private static List<CustomerAddress> ValidateAddresses(IEnumerable<CustomerAddress> addresses)
    {
        if (addresses is null)
        {
            throw new DomainException("At least one address is required.");
        }

        var addressList = addresses.ToList();
        if (addressList.Count == 0)
        {
            throw new DomainException("At least one address is required.");
        }

        if (addressList.Any(address => address is null) || addressList.Count(address => address.IsPrimary) != 1)
        {
            throw new DomainException("Exactly one primary address is required.");
        }

        return addressList;
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
