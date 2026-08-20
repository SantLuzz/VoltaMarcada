using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class Equipment : ICompanyScopedEntity
{
    private Equipment()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid CustomerId { get; private set; }

    public string Type { get; private set; } = string.Empty;

    public string? Manufacturer { get; private set; }

    public string? Model { get; private set; }

    public string? SerialNumber { get; private set; }

    public string? Capacity { get; private set; }

    public string? LocationDescription { get; private set; }

    public DateOnly? InstallationDate { get; private set; }

    public string? Notes { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Equipment Create(
        Guid companyId,
        Guid customerId,
        string type,
        string? manufacturer,
        string? model,
        string? serialNumber,
        string? capacity,
        string? locationDescription,
        DateOnly? installationDate,
        string? notes,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer ID is required.");
        }

        var utcNow = now.ToUniversalTime();
        return new Equipment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            Type = RequireText(type, "Type"),
            Manufacturer = NormalizeOptionalText(manufacturer),
            Model = NormalizeOptionalText(model),
            SerialNumber = NormalizeOptionalText(serialNumber),
            Capacity = NormalizeOptionalText(capacity),
            LocationDescription = NormalizeOptionalText(locationDescription),
            InstallationDate = installationDate,
            Notes = NormalizeOptionalText(notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        Guid customerId,
        string type,
        string? manufacturer,
        string? model,
        string? serialNumber,
        string? capacity,
        string? locationDescription,
        DateOnly? installationDate,
        string? notes,
        DateTimeOffset now)
    {
        if (customerId == Guid.Empty)
        {
            throw new DomainException("Customer ID is required.");
        }

        CustomerId = customerId;
        Type = RequireText(type, "Type");
        Manufacturer = NormalizeOptionalText(manufacturer);
        Model = NormalizeOptionalText(model);
        SerialNumber = NormalizeOptionalText(serialNumber);
        Capacity = NormalizeOptionalText(capacity);
        LocationDescription = NormalizeOptionalText(locationDescription);
        InstallationDate = installationDate;
        Notes = NormalizeOptionalText(notes);
        UpdatedAtUtc = now.ToUniversalTime();
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
