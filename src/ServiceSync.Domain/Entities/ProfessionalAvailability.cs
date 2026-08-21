using ServiceSync.Domain.Abstractions.Tenancy;
using ServiceSync.Domain.Exceptions;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Domain.Entities;

public sealed class ProfessionalAvailability : ICompanyScopedEntity
{
    private readonly List<ProfessionalDailySchedule> _dailySchedules = [];

    private ProfessionalAvailability()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ProfessionalId { get; private set; }

    public IReadOnlyCollection<ProfessionalDailySchedule> DailySchedules => _dailySchedules;

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static ProfessionalAvailability Create(
        Guid companyId,
        Guid professionalId,
        IReadOnlyCollection<ProfessionalDailyScheduleDefinition> dailyScheduleDefinitions,
        DateTimeOffset now)
    {
        EnsureIdentifiers(companyId, professionalId);
        EnsureValidDailySchedules(dailyScheduleDefinitions);
        var utcNow = now.ToUniversalTime();

        var availability = new ProfessionalAvailability
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalId = professionalId,
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };

        availability.ReplaceDailySchedules(dailyScheduleDefinitions);
        return availability;
    }

    public void Update(
        IReadOnlyCollection<ProfessionalDailyScheduleDefinition> dailyScheduleDefinitions,
        DateTimeOffset now)
    {
        EnsureValidDailySchedules(dailyScheduleDefinitions);

        ReplaceDailySchedules(dailyScheduleDefinitions);
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

    private void ReplaceDailySchedules(IReadOnlyCollection<ProfessionalDailyScheduleDefinition> dailyScheduleDefinitions)
    {
        _dailySchedules.Clear();
        _dailySchedules.AddRange(dailyScheduleDefinitions.Select(definition =>
            ProfessionalDailySchedule.Create(CompanyId, Id, definition)));
    }

    private static void EnsureValidDailySchedules(
        IReadOnlyCollection<ProfessionalDailyScheduleDefinition> dailyScheduleDefinitions)
    {
        if (dailyScheduleDefinitions.Count == 0)
        {
            throw new DomainException("At least one daily schedule is required.");
        }

        if (dailyScheduleDefinitions.Any(definition => !Enum.IsDefined(definition.Weekday)))
        {
            throw new DomainException("Each daily schedule must have a valid weekday.");
        }

        if (dailyScheduleDefinitions.Select(definition => definition.Weekday).Distinct().Count() != dailyScheduleDefinitions.Count)
        {
            throw new DomainException("Each weekday can have only one daily schedule.");
        }

        foreach (var definition in dailyScheduleDefinitions)
        {
            if (definition.EntryTime >= definition.ExitTime)
            {
                throw new DomainException("Daily schedule entry time must be earlier than exit time.");
            }

            if (definition.LunchStartTime.HasValue != definition.LunchEndTime.HasValue)
            {
                throw new DomainException("Daily schedule lunch times must both be provided or both be empty.");
            }

            if (definition.LunchStartTime is { } lunchStartTime &&
                definition.LunchEndTime is { } lunchEndTime &&
                (definition.EntryTime >= lunchStartTime ||
                 lunchStartTime >= lunchEndTime ||
                 lunchEndTime >= definition.ExitTime))
            {
                throw new DomainException("Daily schedule lunch times must be within work hours.");
            }
        }
    }
}
