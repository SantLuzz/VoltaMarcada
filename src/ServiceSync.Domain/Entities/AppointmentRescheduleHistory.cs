using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class AppointmentRescheduleHistory : ICompanyScopedEntity
{
    private AppointmentRescheduleHistory()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid AppointmentId { get; private set; }
    public DateTimeOffset PreviousStartAtUtc { get; private set; }
    public DateTimeOffset PreviousEndAtUtc { get; private set; }
    public DateTimeOffset NewStartAtUtc { get; private set; }
    public DateTimeOffset NewEndAtUtc { get; private set; }
    public string Reason { get; private set; } = null!;
    public Guid? ChangedByUserId { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    internal static AppointmentRescheduleHistory Create(
        Guid companyId,
        Guid appointmentId,
        DateTimeOffset previousStartAtUtc,
        DateTimeOffset previousEndAtUtc,
        DateTimeOffset newStartAtUtc,
        DateTimeOffset newEndAtUtc,
        string reason,
        Guid? changedByUserId,
        DateTimeOffset now)
    {
        if (companyId == Guid.Empty || appointmentId == Guid.Empty)
        {
            throw new DomainException("Company ID and appointment ID are required.");
        }

        var normalizedReason = reason?.Trim();
        if (string.IsNullOrWhiteSpace(normalizedReason) || normalizedReason.Length > 1000)
        {
            throw new DomainException("Reschedule reason is required and cannot exceed 1000 characters.");
        }

        return new AppointmentRescheduleHistory
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            AppointmentId = appointmentId,
            PreviousStartAtUtc = previousStartAtUtc.ToUniversalTime(),
            PreviousEndAtUtc = previousEndAtUtc.ToUniversalTime(),
            NewStartAtUtc = newStartAtUtc.ToUniversalTime(),
            NewEndAtUtc = newEndAtUtc.ToUniversalTime(),
            Reason = normalizedReason,
            ChangedByUserId = changedByUserId,
            CreatedAtUtc = now.ToUniversalTime()
        };
    }
}
