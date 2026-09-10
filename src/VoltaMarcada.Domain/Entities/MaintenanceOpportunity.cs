using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class MaintenanceOpportunity : ICompanyScopedEntity
{
    private MaintenanceOpportunity()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid EquipmentId { get; private set; }
    public Guid? PreviousWorkOrderId { get; private set; }
    public Guid RecommendedServiceId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public MaintenanceOpportunityStatus Status { get; private set; }
    public MaintenanceOpportunityConversionType? ConversionType { get; private set; }
    public Guid? ConvertedEntityId { get; private set; }
    public DateTimeOffset? ConvertedAtUtc { get; private set; }
    public DateTimeOffset? ClosedAtUtc { get; private set; }
    public Guid? ClosedByUserId { get; private set; }
    public string? ClosureReason { get; private set; }
    public DateTimeOffset? ContactedAtUtc { get; private set; }
    public DateTimeOffset? BookingRequestedAtUtc { get; private set; }
    public DateTimeOffset? BookedAtUtc { get; private set; }
    public DateTimeOffset? CompletedAtUtc { get; private set; }
    public DateTimeOffset? DismissedAtUtc { get; private set; }
    public string? BookingTokenHash { get; private set; }
    public DateTimeOffset? BookingTokenExpiresAtUtc { get; private set; }
    public DateTimeOffset? BookingTokenRevokedAtUtc { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static MaintenanceOpportunity Create(
        Guid companyId,
        Guid customerId,
        Guid equipmentId,
        Guid? previousWorkOrderId,
        Guid recommendedServiceId,
        DateOnly dueDate,
        string notes,
        DateTimeOffset now)
    {
        RequireId(companyId, "Company");
        RequireId(customerId, "Customer");
        RequireId(equipmentId, "Equipment");
        if (previousWorkOrderId is { } workOrderId)
        {
            RequireId(workOrderId, "Previous work order");
        }
        RequireId(recommendedServiceId, "Recommended service");
        if (dueDate == DateOnly.MinValue)
        {
            throw new DomainException("Due date is required.");
        }

        var utcNow = now.ToUniversalTime();
        return new MaintenanceOpportunity
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            CustomerId = customerId,
            EquipmentId = equipmentId,
            PreviousWorkOrderId = previousWorkOrderId,
            RecommendedServiceId = recommendedServiceId,
            DueDate = dueDate,
            Notes = NormalizeNotes(notes),
            Status = MaintenanceOpportunityStatus.Pending,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public static MaintenanceOpportunity CreateManual(
        Guid companyId,
        Guid customerId,
        Guid equipmentId,
        Guid recommendedServiceId,
        DateOnly dueDate,
        string notes,
        DateTimeOffset now) =>
        Create(companyId, customerId, equipmentId, null, recommendedServiceId, dueDate, notes, now);

    public void MarkAsContacted(DateTimeOffset now)
    {
        EnsureOpen();
        if (Status == MaintenanceOpportunityStatus.AwaitingCustomer)
        {
            return;
        }

        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.Contacted;
        ContactedAtUtc ??= utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkAsAwaitingCustomer(DateTimeOffset now)
    {
        EnsureOpen();
        if (Status == MaintenanceOpportunityStatus.AwaitingCustomer)
        {
            return;
        }

        if (Status is not MaintenanceOpportunityStatus.Pending and not MaintenanceOpportunityStatus.Contacted)
        {
            throw new DomainException("Maintenance opportunity cannot wait for customer in its current status.");
        }

        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.AwaitingCustomer;
        BookingRequestedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Convert(MaintenanceOpportunityConversionType conversionType, Guid convertedEntityId, DateTimeOffset now)
    {
        EnsureOpen();
        if (!Enum.IsDefined(conversionType) || convertedEntityId == Guid.Empty)
        {
            throw new DomainException("Maintenance opportunity conversion is invalid.");
        }

        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.Converted;
        ConversionType = conversionType;
        ConvertedEntityId = convertedEntityId;
        ConvertedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void CloseWithoutConversion(Guid closedByUserId, string reason, DateTimeOffset now)
    {
        EnsureOpen();
        if (closedByUserId == Guid.Empty)
        {
            throw new DomainException("Maintenance opportunity closing user ID is required.");
        }

        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.ClosedWithoutConversion;
        ClosedAtUtc = utcNow;
        ClosedByUserId = closedByUserId;
        ClosureReason = NormalizeClosureReason(reason);
        UpdatedAtUtc = utcNow;
    }

    public void SetBookingToken(string tokenHash, DateTimeOffset expiresAtUtc, DateTimeOffset now)
    {
        EnsureOpen();
        if (string.IsNullOrWhiteSpace(tokenHash) || tokenHash.Trim().Length > 128 || expiresAtUtc <= now)
        {
            throw new DomainException("Maintenance booking token is invalid.");
        }

        if (Status is MaintenanceOpportunityStatus.Pending or MaintenanceOpportunityStatus.Contacted)
        {
            MarkAsAwaitingCustomer(now);
        }

        if (Status != MaintenanceOpportunityStatus.AwaitingCustomer)
        {
            throw new DomainException("Maintenance opportunity cannot receive a booking token in its current status.");
        }

        BookingTokenHash = tokenHash.Trim();
        BookingTokenExpiresAtUtc = expiresAtUtc.ToUniversalTime();
        BookingTokenRevokedAtUtc = null;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void RevokeBookingToken(DateTimeOffset now)
    {
        EnsureOpen();
        if (BookingTokenHash is null)
        {
            throw new DomainException("Maintenance booking token does not exist.");
        }

        BookingTokenRevokedAtUtc = now.ToUniversalTime();
        if (Status == MaintenanceOpportunityStatus.AwaitingCustomer)
        {
            Status = MaintenanceOpportunityStatus.Contacted;
        }

        UpdatedAtUtc = now.ToUniversalTime();
    }

    private void EnsureOpen()
    {
        if (Status is MaintenanceOpportunityStatus.Converted or MaintenanceOpportunityStatus.ClosedWithoutConversion)
        {
            throw new DomainException("Maintenance opportunity is already closed.");
        }
    }

    private static void RequireId(Guid value, string name)
    {
        if (value == Guid.Empty)
        {
            throw new DomainException($"{name} ID is required.");
        }
    }

    private static string NormalizeNotes(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null || normalized.Length > 2000)
        {
            throw new DomainException("Maintenance opportunity notes are required and cannot exceed 2000 characters.");
        }

        return normalized;
    }

    private static string NormalizeClosureReason(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized is null || normalized.Length > 2000)
        {
            throw new DomainException("Maintenance opportunity closure reason is required and cannot exceed 2000 characters.");
        }

        return normalized;
    }
}
