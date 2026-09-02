using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class Appointment : ICompanyScopedEntity
{
    private readonly List<AppointmentRescheduleHistory> _rescheduleHistory = [];

    private Appointment()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public Guid? CustomerId { get; private set; }
    public Guid? EquipmentId { get; private set; }
    public Guid? ServiceId { get; private set; }
    public Guid? WorkOrderId { get; private set; }
    public DateTimeOffset StartAtUtc { get; private set; }
    public DateTimeOffset EndAtUtc { get; private set; }
    public AppointmentStatus Status { get; private set; }
    public AppointmentOrigin Origin { get; private set; }
    public AppointmentType Type { get; private set; }
    public string? Description { get; private set; }
    public string? Notes { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset UpdatedAtUtc { get; private set; }
    public IReadOnlyCollection<AppointmentRescheduleHistory> RescheduleHistory => _rescheduleHistory;

    public static Appointment Create(
        Guid companyId,
        Guid professionalId,
        Guid? customerId,
        Guid? equipmentId,
        Guid? serviceId,
        Guid? workOrderId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        string? notes,
        DateTimeOffset now,
        AppointmentOrigin origin = AppointmentOrigin.Internal,
        AppointmentType type = AppointmentType.Other,
        string? description = null)
    {
        EnsureIdentifiers(companyId, professionalId);
        EnsureValidInterval(startAtUtc, endAtUtc);
        if (!Enum.IsDefined(origin))
        {
            throw new DomainException("Appointment origin is invalid.");
        }
        if (!Enum.IsDefined(type)) throw new DomainException("Appointment type is invalid.");
        if (type != AppointmentType.Service && NormalizeOptionalId(workOrderId) is not null) throw new DomainException("Only a service appointment can be linked to a work order.");
        var utcNow = now.ToUniversalTime();

        return new Appointment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalId = professionalId,
            CustomerId = NormalizeOptionalId(customerId),
            EquipmentId = NormalizeOptionalId(equipmentId),
            ServiceId = NormalizeOptionalId(serviceId),
            WorkOrderId = NormalizeOptionalId(workOrderId),
            StartAtUtc = startAtUtc.ToUniversalTime(),
            EndAtUtc = endAtUtc.ToUniversalTime(),
            Status = AppointmentStatus.Scheduled,
            Origin = origin,
            Type = type,
            Description = NormalizeDescription(description),
            Notes = NormalizeNotes(notes),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Reschedule(DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, string reason, Guid? changedByUserId, DateTimeOffset now)
    {
        EnsureScheduled();
        EnsureValidInterval(startAtUtc, endAtUtc);
        var utcNow = now.ToUniversalTime();
        var normalizedStart = startAtUtc.ToUniversalTime();
        var normalizedEnd = endAtUtc.ToUniversalTime();
        _rescheduleHistory.Add(AppointmentRescheduleHistory.Create(CompanyId, Id, StartAtUtc, EndAtUtc, normalizedStart, normalizedEnd, reason, changedByUserId, utcNow));
        StartAtUtc = normalizedStart;
        EndAtUtc = normalizedEnd;
        UpdatedAtUtc = utcNow;
    }

    public void Reassign(Guid professionalId, DateTimeOffset now)
    {
        EnsureScheduled();
        if (professionalId == Guid.Empty)
        {
            throw new DomainException("Professional ID is required.");
        }

        ProfessionalId = professionalId;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void UpdateWorkOrderAssociation(Guid? workOrderId, Guid? customerId, Guid? equipmentId, Guid? serviceId, DateTimeOffset now)
    {
        EnsureScheduled();
        if (NormalizeOptionalId(workOrderId) is not null && Type != AppointmentType.Service)
        {
            throw new DomainException("Only a service appointment can be linked to a work order.");
        }

        WorkOrderId = NormalizeOptionalId(workOrderId);
        CustomerId = NormalizeOptionalId(customerId);
        EquipmentId = NormalizeOptionalId(equipmentId);
        ServiceId = NormalizeOptionalId(serviceId);
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void UpdateNotes(string? notes, DateTimeOffset now)
    {
        EnsureScheduled();
        Notes = NormalizeNotes(notes);
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Complete(DateTimeOffset now)
    {
        EnsureInProgress();
        Status = AppointmentStatus.Completed;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Start(DateTimeOffset now)
    {
        EnsureScheduled();
        Status = AppointmentStatus.InProgress;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Cancel(DateTimeOffset now)
    {
        EnsureOpen();
        Status = AppointmentStatus.Cancelled;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    private static void EnsureIdentifiers(Guid companyId, Guid professionalId)
    {
        if (companyId == Guid.Empty || professionalId == Guid.Empty)
        {
            throw new DomainException("Company ID and professional ID are required.");
        }
    }

    private static void EnsureValidInterval(DateTimeOffset startAtUtc, DateTimeOffset endAtUtc)
    {
        if (endAtUtc <= startAtUtc)
        {
            throw new DomainException("Appointment end must be after appointment start.");
        }
    }

    private void EnsureScheduled()
    {
        if (Status != AppointmentStatus.Scheduled)
        {
            throw new DomainException("Only scheduled appointments can be changed.");
        }
    }

    private void EnsureInProgress()
    {
        if (Status != AppointmentStatus.InProgress)
        {
            throw new DomainException("Only in-progress appointments can be completed.");
        }
    }

    private void EnsureOpen()
    {
        if (Status is not AppointmentStatus.Scheduled and not AppointmentStatus.InProgress)
        {
            throw new DomainException("Only open appointments can be cancelled.");
        }
    }

    private static Guid? NormalizeOptionalId(Guid? value) => value is null || value == Guid.Empty ? null : value;

    private static string? NormalizeNotes(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 1000)
        {
            throw new DomainException("Appointment notes cannot exceed 1000 characters.");
        }

        return normalized;
    }

    private static string? NormalizeDescription(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 1000) throw new DomainException("Appointment description cannot exceed 1000 characters.");
        return normalized;
    }
}
