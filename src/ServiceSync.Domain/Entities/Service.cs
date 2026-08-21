using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class Service : ICompanyScopedEntity
{
    private readonly List<ServiceChecklistItem> _checklistItems = [];

    private Service()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public int DurationMinutes { get; private set; }

    public ServicePriceType PriceType { get; private set; }

    public decimal? BasePrice { get; private set; }

    public bool AllowOnlineBooking { get; private set; }

    public bool RequireBeforePhotos { get; private set; }

    public bool RequireAfterPhotos { get; private set; }

    public bool IsActive { get; private set; }

    public IReadOnlyCollection<ServiceChecklistItem> ChecklistItems => _checklistItems;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static Service Create(
        Guid companyId,
        string name,
        string? description,
        int durationMinutes,
        ServicePriceType priceType,
        decimal? basePrice,
        bool allowOnlineBooking,
        bool requireBeforePhotos,
        bool requireAfterPhotos,
        IEnumerable<ServiceChecklistItem> checklistItems,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        var utcNow = now.ToUniversalTime();
        var service = new Service
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Name = RequireText(name, "Name"),
            Description = NormalizeOptionalText(description),
            DurationMinutes = ValidateDuration(durationMinutes),
            PriceType = ValidatePriceType(priceType),
            BasePrice = ValidateBasePrice(priceType, basePrice),
            AllowOnlineBooking = allowOnlineBooking,
            RequireBeforePhotos = requireBeforePhotos,
            RequireAfterPhotos = requireAfterPhotos,
            IsActive = true,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        service.ReplaceChecklistItems(checklistItems);
        return service;
    }

    public void Update(
        string name,
        string? description,
        int durationMinutes,
        ServicePriceType priceType,
        decimal? basePrice,
        bool allowOnlineBooking,
        bool requireBeforePhotos,
        bool requireAfterPhotos,
        IEnumerable<ServiceChecklistItem> checklistItems,
        DateTimeOffset now)
    {
        Name = RequireText(name, "Name");
        Description = NormalizeOptionalText(description);
        DurationMinutes = ValidateDuration(durationMinutes);
        PriceType = ValidatePriceType(priceType);
        BasePrice = ValidateBasePrice(priceType, basePrice);
        AllowOnlineBooking = allowOnlineBooking;
        RequireBeforePhotos = requireBeforePhotos;
        RequireAfterPhotos = requireAfterPhotos;
        ReplaceChecklistItems(checklistItems);
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Activate(DateTimeOffset now)
    {
        IsActive = true;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Deactivate(DateTimeOffset now)
    {
        IsActive = false;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    private void ReplaceChecklistItems(IEnumerable<ServiceChecklistItem> checklistItems)
    {
        if (checklistItems is null)
        {
            throw new DomainException("Checklist items are required.");
        }

        var normalizedItems = checklistItems
            .Select((item, index) =>
            {
                if (item is null)
                {
                    throw new DomainException("Checklist item is required.");
                }

                return ServiceChecklistItem.Create(item.Description, item.IsRequired, index);
            })
            .ToArray();

        foreach (var item in normalizedItems)
        {
            item.AssignToService(CompanyId, Id);
        }

        _checklistItems.Clear();
        _checklistItems.AddRange(normalizedItems);
    }

    private static int ValidateDuration(int durationMinutes)
    {
        if (durationMinutes <= 0)
        {
            throw new DomainException("Duration minutes must be greater than zero.");
        }

        return durationMinutes;
    }

    private static ServicePriceType ValidatePriceType(ServicePriceType priceType)
    {
        if (!Enum.IsDefined(priceType))
        {
            throw new DomainException("Service price type is invalid.");
        }

        return priceType;
    }

    private static decimal? ValidateBasePrice(ServicePriceType priceType, decimal? basePrice)
    {
        var requiresBasePrice = priceType is ServicePriceType.Fixed or ServicePriceType.StartingAt;
        if (requiresBasePrice && basePrice is null)
        {
            throw new DomainException("Base price is required for the selected price type.");
        }

        if (!requiresBasePrice && basePrice is not null)
        {
            throw new DomainException("Base price is not allowed for the selected price type.");
        }

        if (basePrice is < 0)
        {
            throw new DomainException("Base price cannot be negative.");
        }

        return basePrice;
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
