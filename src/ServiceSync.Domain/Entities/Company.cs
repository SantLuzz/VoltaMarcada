using ServiceSync.Domain.Exceptions;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Domain.Entities;

public sealed class Company
{
    private Company()
    {
    }

    public Guid Id { get; private set; }

    public string LegalName { get; private set; } = string.Empty;

    public string TradeName { get; private set; } = string.Empty;

    public string Document { get; private set; } = string.Empty;

    public string Phone { get; private set; } = string.Empty;

    public string WhatsApp { get; private set; } = string.Empty;

    public string City { get; private set; } = string.Empty;

    public string State { get; private set; } = string.Empty;

    public string? LogoKey { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Company Create(
        string legalName,
        string tradeName,
        string document,
        string phone,
        string whatsApp,
        string city,
        string state,
        string? logoKey,
        string slug,
        DateTimeOffset now)
    {
        if (!BrazilianDocument.TryNormalize(document, out var normalizedDocument))
        {
            throw new DomainException("A valid Brazilian document is required.");
        }

        return new Company
        {
            Id = Guid.NewGuid(),
            LegalName = RequireText(legalName, "Legal name"),
            TradeName = RequireText(tradeName, "Trade name"),
            Document = normalizedDocument,
            Phone = RequireText(phone, "Phone"),
            WhatsApp = RequireText(whatsApp, "WhatsApp"),
            City = RequireText(city, "City"),
            State = RequireText(state, "State"),
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
