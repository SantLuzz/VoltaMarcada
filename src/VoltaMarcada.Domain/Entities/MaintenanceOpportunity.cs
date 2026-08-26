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
    public Guid PreviousWorkOrderId { get; private set; }
    public Guid RecommendedServiceId { get; private set; }
    public DateOnly DueDate { get; private set; }
    public string Notes { get; private set; } = string.Empty;
    public MaintenanceOpportunityStatus Status { get; private set; }
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
        Guid previousWorkOrderId,
        Guid recommendedServiceId,
        DateOnly dueDate,
        string notes,
        DateTimeOffset now)
    {
        RequireId(companyId, "Company");
        RequireId(customerId, "Customer");
        RequireId(equipmentId, "Equipment");
        RequireId(previousWorkOrderId, "Previous work order");
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

    public void MarkAsContacted(DateTimeOffset now)
    {
        if (Status == MaintenanceOpportunityStatus.Contacted)
        {
            return;
        }

        EnsureOpen();
        if (Status != MaintenanceOpportunityStatus.Pending)
        {
            throw new DomainException("Maintenance opportunity cannot be marked as contacted in its current status.");
        }

        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.Contacted;
        ContactedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkAsBookingRequested(DateTimeOffset now)
    {
        EnsureOpen();
        if (Status is not MaintenanceOpportunityStatus.Pending and not MaintenanceOpportunityStatus.Contacted)
        {
            throw new DomainException("Maintenance opportunity cannot request booking in its current status.");
        }

        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.BookingRequested;
        BookingRequestedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void MarkAsBooked(DateTimeOffset now)
    {
        EnsureStatus(MaintenanceOpportunityStatus.BookingRequested);
        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.Booked;
        BookedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureStatus(MaintenanceOpportunityStatus.Booked);
        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.Completed;
        CompletedAtUtc = utcNow;
        UpdatedAtUtc = utcNow;
    }

    public void Dismiss(DateTimeOffset now)
    {
        EnsureOpen();
        var utcNow = now.ToUniversalTime();
        Status = MaintenanceOpportunityStatus.Dismissed;
        DismissedAtUtc = utcNow;
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
            MarkAsBookingRequested(now);
        }

        if (Status != MaintenanceOpportunityStatus.BookingRequested)
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
        UpdatedAtUtc = now.ToUniversalTime();
    }

    private void EnsureOpen()
    {
        if (Status is MaintenanceOpportunityStatus.Completed or MaintenanceOpportunityStatus.Dismissed)
        {
            throw new DomainException("Maintenance opportunity is already closed.");
        }
    }

    private void EnsureStatus(MaintenanceOpportunityStatus expectedStatus)
    {
        if (Status != expectedStatus)
        {
            throw new DomainException("Maintenance opportunity is not in the required status.");
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
}
