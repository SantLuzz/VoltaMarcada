using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class Company
{
    private readonly List<CompanyBusinessHours> businessHours = [];
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

    public string? PublicDescription { get; private set; }

    public string? PublicPhone { get; private set; }

    public string? NotificationEmail { get; private set; }

    public int PublicRequestIntervalMinutes { get; private set; } = 60;

    public bool PaymentControlEnabled { get; private set; }

    public string? PixKey { get; private set; }

    public PixKeyType? PixKeyType { get; private set; }

    public IReadOnlyCollection<CompanyBusinessHours> BusinessHours => businessHours.AsReadOnly();

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

        var company = new Company
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

        company.InitializeDefaultBusinessHours();
        return company;
    }

    public void UpdatePublicProfile(string? publicDescription, string? publicPhone, string? notificationEmail, DateTimeOffset now)
    {
        PublicDescription = NormalizeOptionalText(publicDescription, 2000, "Public description");
        PublicPhone = NormalizeOptionalText(publicPhone, 20, "Public phone");
        NotificationEmail = NormalizeEmail(notificationEmail);
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void UpdatePaymentSettings(bool paymentControlEnabled, string? pixKey, PixKeyType? pixKeyType, DateTimeOffset now)
    {
        var normalizedPixKey = NormalizeOptionalText(pixKey, 255, "Pix key");
        if ((normalizedPixKey is null) != (pixKeyType is null))
        {
            throw new DomainException("Pix key and key type must be provided together.");
        }

        if (pixKeyType is { } type && !Enum.IsDefined(type)) throw new DomainException("Pix key type is invalid.");
        PaymentControlEnabled = paymentControlEnabled;
        PixKey = normalizedPixKey;
        PixKeyType = pixKeyType;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void UpdatePublicScheduling(int publicRequestIntervalMinutes, IReadOnlyCollection<CompanyBusinessHoursInput> hours, DateTimeOffset now)
    {
        if (publicRequestIntervalMinutes <= 0 || 60 % publicRequestIntervalMinutes != 0)
        {
            throw new DomainException("Public request interval must divide one hour.");
        }

        if (hours.Count != 7 || hours.Select(item => item.DayOfWeek).Distinct().Count() != 7)
        {
            throw new DomainException("Business hours must contain each day of the week exactly once.");
        }

        foreach (var input in hours)
        {
            var current = businessHours.SingleOrDefault(item => item.DayOfWeek == input.DayOfWeek);
            if (current is null)
            {
                businessHours.Add(CompanyBusinessHours.Create(Id, input.DayOfWeek, input.IsOpen, input.StartTime, input.EndTime));
            }
            else
            {
                current.Update(input.IsOpen, input.StartTime, input.EndTime);
            }
        }

        PublicRequestIntervalMinutes = publicRequestIntervalMinutes;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public bool IsPublicRequestSlot(DateOnly date, TimeOnly startTime)
    {
        var hours = businessHours.SingleOrDefault(item => item.DayOfWeek == date.DayOfWeek);
        return hours is { IsOpen: true, StartTime: { } openingTime, EndTime: { } closingTime } &&
               startTime >= openingTime && startTime < closingTime &&
               (startTime.Minute * 60 + startTime.Second) == 0 &&
               (startTime.Hour * 60 + startTime.Minute - openingTime.Hour * 60 - openingTime.Minute) % PublicRequestIntervalMinutes == 0;
    }

    private void InitializeDefaultBusinessHours()
    {
        foreach (var dayOfWeek in Enum.GetValues<DayOfWeek>())
        {
            var isOpen = dayOfWeek is >= DayOfWeek.Monday and <= DayOfWeek.Friday;
            businessHours.Add(CompanyBusinessHours.Create(
                Id,
                dayOfWeek,
                isOpen,
                isOpen ? new TimeOnly(8, 0) : null,
                isOpen ? new TimeOnly(18, 0) : null));
        }
    }

    private static string RequireText(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new DomainException($"{fieldName} is required.");
        }

        return value.Trim();
    }

    private static string? NormalizeOptionalText(string? value, int? maximumLength = null, string? fieldName = null)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is not null && maximumLength is { } maxLength && normalized.Length > maxLength)
        {
            throw new DomainException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return normalized;
    }

    private static string? NormalizeEmail(string? value)
    {
        var normalized = NormalizeOptionalText(value, 320, "Notification email");
        if (normalized is not null && (!System.Net.Mail.MailAddress.TryCreate(normalized, out var address) || !string.Equals(address.Address, normalized, StringComparison.OrdinalIgnoreCase)))
        {
            throw new DomainException("Notification email is invalid.");
        }

        return normalized;
    }
}
