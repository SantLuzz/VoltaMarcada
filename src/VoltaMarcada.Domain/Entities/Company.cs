using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class Company
{
    private Company()
    {
    }

    public Guid Id { get; private set; }

    public string LegalName { get; private set; } = string.Empty;

    public string TradeName { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string WhatsApp { get; private set; } = string.Empty;

    public string PostalCode { get; private set; } = string.Empty;

    public string Address { get; private set; } = string.Empty;

    public Guid StateId { get; private set; }

    public Guid CityId { get; private set; }

    public string? LogoKey { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Company Create(
        string legalName,
        string tradeName,
        string document,
        string whatsApp,
        string postalCode,
        string address,
        Guid stateId,
        Guid cityId,
        string? logoKey,
        string slug,
        DateTimeOffset now)
    {
        if (!BrazilianDocument.TryNormalize(document, out var normalizedDocument))
        {
            throw new DomainException("A valid Brazilian document is required.");
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

        return new Company
        {
            Id = Guid.NewGuid(),
            LegalName = RequireText(legalName, "Legal name"),
            TradeName = RequireText(tradeName, "Trade name"),
            Document = normalizedDocument,
            WhatsApp = RequireText(whatsApp, "WhatsApp"),
            PostalCode = normalizedPostalCode,
            Address = RequireText(address, "Address"),
            StateId = stateId,
            CityId = cityId,
            LogoKey = NormalizeOptionalText(logoKey),
            Slug = RequireText(slug, "Slug"),
            CreatedAtUtc = now.ToUniversalTime(),
            UpdatedAtUtc = now.ToUniversalTime()
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
