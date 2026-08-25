using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class ScheduleBlock : ICompanyScopedEntity
{
    public const int MaxNotesLength = 1000;

    private ScheduleBlock()
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

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public static ScheduleBlock Create(
        Guid companyId,
        Guid professionalId,
        ScheduleBlockType type,
        DateOnly startDate,
        DateOnly endDate,
        bool isFullDay,
        TimeOnly? startTime,
        TimeOnly? endTime,
        string? notes,
        DateTimeOffset now)
    {
        EnsureIdentifiers(companyId, professionalId);
        EnsureValidPeriod(type, startDate, endDate, isFullDay, startTime, endTime);
        var normalizedNotes = NormalizeNotes(notes);

        return new ScheduleBlock
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalId = professionalId,
            Type = type,
            StartDate = startDate,
            EndDate = endDate,
            IsFullDay = isFullDay,
            StartTime = isFullDay ? null : startTime,
            EndTime = isFullDay ? null : endTime,
            Notes = normalizedNotes,
            CreatedAtUtc = now.ToUniversalTime()
        };
    }

    private static void EnsureIdentifiers(Guid companyId, Guid professionalId)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (professionalId == Guid.Empty)
        {
            throw new DomainException("Professional ID is required.");
        }
    }

    private static void EnsureValidPeriod(
        ScheduleBlockType type,
        DateOnly startDate,
        DateOnly endDate,
        bool isFullDay,
        TimeOnly? startTime,
        TimeOnly? endTime)
    {
        if (!Enum.IsDefined(type))
        {
            throw new DomainException("Schedule block type is invalid.");
        }

        if (endDate < startDate)
        {
            throw new DomainException("Schedule block end date cannot be earlier than its start date.");
        }

        if (isFullDay)
        {
            return;
        }

        if (startDate != endDate)
        {
            throw new DomainException("Timed schedule blocks must occur on a single date.");
        }

        if (startTime is null || endTime is null || startTime >= endTime)
        {
            throw new DomainException("Timed schedule blocks require a valid start and end time.");
        }
    }

    private static string? NormalizeNotes(string? notes)
    {
        var normalizedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (normalizedNotes?.Length > MaxNotesLength)
        {
            throw new DomainException($"Schedule block notes cannot exceed {MaxNotesLength} characters.");
        }

        return normalizedNotes;
    }
}
