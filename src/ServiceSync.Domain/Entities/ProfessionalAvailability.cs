using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.Exceptions;

namespace ServiceSync.Domain.Entities;

public sealed class ProfessionalAvailability : ICompanyScopedEntity
{
    private ProfessionalAvailability()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ProfessionalId { get; private set; }

    public ProfessionalWorkDays WorkDays { get; private set; }

    public TimeOnly EntryTime { get; private set; }

    public TimeOnly LunchStartTime { get; private set; }

    public TimeOnly LunchEndTime { get; private set; }

    public TimeOnly ExitTime { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ProfessionalAvailability Create(
        Guid companyId,
        Guid professionalId,
        ProfessionalWorkDays workDays,
        TimeOnly entryTime,
        TimeOnly lunchStartTime,
        TimeOnly lunchEndTime,
        TimeOnly exitTime,
        DateTimeOffset now)
    {
        EnsureIdentifiers(companyId, professionalId);
        EnsureValidSchedule(workDays, entryTime, lunchStartTime, lunchEndTime, exitTime);
        var utcNow = now.ToUniversalTime();

        return new ProfessionalAvailability
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalId = professionalId,
            WorkDays = workDays,
            EntryTime = entryTime,
            LunchStartTime = lunchStartTime,
            LunchEndTime = lunchEndTime,
            ExitTime = exitTime,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void Update(
        ProfessionalWorkDays workDays,
        TimeOnly entryTime,
        TimeOnly lunchStartTime,
        TimeOnly lunchEndTime,
        TimeOnly exitTime,
        DateTimeOffset now)
    {
        EnsureValidSchedule(workDays, entryTime, lunchStartTime, lunchEndTime, exitTime);

        WorkDays = workDays;
        EntryTime = entryTime;
        LunchStartTime = lunchStartTime;
        LunchEndTime = lunchEndTime;
        ExitTime = exitTime;
        UpdatedAtUtc = now.ToUniversalTime();
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

    private static void EnsureValidSchedule(
        ProfessionalWorkDays workDays,
        TimeOnly entryTime,
        TimeOnly lunchStartTime,
        TimeOnly lunchEndTime,
        TimeOnly exitTime)
    {
        if (workDays == ProfessionalWorkDays.None || (workDays & ~ProfessionalWorkDays.All) != 0)
        {
            throw new DomainException("At least one valid workday is required.");
        }

        if (entryTime >= lunchStartTime || lunchStartTime >= lunchEndTime || lunchEndTime >= exitTime)
        {
            throw new DomainException("Work schedule times must follow entry, lunch start, lunch end, and exit order.");
        }
    }
}
