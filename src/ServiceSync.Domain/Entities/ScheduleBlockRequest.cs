using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class ScheduleBlockRequest : ICompanyScopedEntity
{
    private ScheduleBlockRequest()
    {
    }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid ProfessionalId { get; private set; }
    public ScheduleBlockType Type { get; private set; }
    public DateOnly StartDate { get; private set; }
    public DateOnly EndDate { get; private set; }
    public bool IsFullDay { get; private set; }
    public TimeOnly? StartTime { get; private set; }
    public TimeOnly? EndTime { get; private set; }
    public string? Notes { get; private set; }
    public ScheduleBlockRequestStatus Status { get; private set; }
    public Guid? DecidedByUserId { get; private set; }
    public DateTimeOffset? DecidedAtUtc { get; private set; }
    public string? RejectionReason { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ScheduleBlockRequest Create(Guid companyId, Guid professionalId, ScheduleBlockType type, DateOnly startDate, DateOnly endDate, bool isFullDay, TimeOnly? startTime, TimeOnly? endTime, string? notes, DateTimeOffset now)
    {
        if (companyId == Guid.Empty || professionalId == Guid.Empty)
        {
            throw new DomainException("Company ID and professional ID are required.");
        }

        EnsurePeriod(type, startDate, endDate, isFullDay, startTime, endTime);
        return new ScheduleBlockRequest
        {
            Id = Guid.NewGuid(), CompanyId = companyId, ProfessionalId = professionalId, Type = type,
            StartDate = startDate, EndDate = endDate, IsFullDay = isFullDay,
            StartTime = isFullDay ? null : startTime, EndTime = isFullDay ? null : endTime,
            Notes = NormalizeNotes(notes), Status = ScheduleBlockRequestStatus.Pending, CreatedAtUtc = now.ToUniversalTime()
        };
    }

    public void RequireAppointmentResolution()
    {
        if (Status is ScheduleBlockRequestStatus.Pending or ScheduleBlockRequestStatus.RequiresAppointmentResolution)
        {
            Status = ScheduleBlockRequestStatus.RequiresAppointmentResolution;
        }
    }

    public void MarkPending()
    {
        if (Status == ScheduleBlockRequestStatus.RequiresAppointmentResolution)
        {
            Status = ScheduleBlockRequestStatus.Pending;
        }
    }

    public void Approve(Guid decidedByUserId, DateTimeOffset now)
    {
        EnsureAwaitingDecision();
        if (decidedByUserId == Guid.Empty)
        {
            throw new DomainException("Deciding user ID is required.");
        }

        Status = ScheduleBlockRequestStatus.Approved;
        DecidedByUserId = decidedByUserId;
        DecidedAtUtc = now.ToUniversalTime();
        RejectionReason = null;
    }

    public void Reject(Guid decidedByUserId, string? reason, DateTimeOffset now)
    {
        EnsureAwaitingDecision();
        if (decidedByUserId == Guid.Empty)
        {
            throw new DomainException("Deciding user ID is required.");
        }

        Status = ScheduleBlockRequestStatus.Rejected;
        DecidedByUserId = decidedByUserId;
        DecidedAtUtc = now.ToUniversalTime();
        RejectionReason = NormalizeNotes(reason);
    }

    private void EnsureAwaitingDecision()
    {
        if (Status is not (ScheduleBlockRequestStatus.Pending or ScheduleBlockRequestStatus.RequiresAppointmentResolution))
        {
            throw new DomainException("Only pending or resolution-required block requests can be decided.");
        }
    }

    private static void EnsurePeriod(ScheduleBlockType type, DateOnly startDate, DateOnly endDate, bool isFullDay, TimeOnly? startTime, TimeOnly? endTime)
    {
        if (!Enum.IsDefined(type) || endDate < startDate)
        {
            throw new DomainException("Schedule block request period is invalid.");
        }

        if (!isFullDay && (startDate != endDate || startTime is null || endTime is null || startTime >= endTime))
        {
            throw new DomainException("Timed block requests require one date and a valid interval.");
        }
    }

    private static string? NormalizeNotes(string? value)
    {
        var normalized = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
        if (normalized?.Length > 1000)
        {
            throw new DomainException("Schedule block request notes cannot exceed 1000 characters.");
        }

        return normalized;
    }
}
