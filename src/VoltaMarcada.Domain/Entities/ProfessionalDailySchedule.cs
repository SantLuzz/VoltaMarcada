using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Domain.Entities;

public sealed class ProfessionalDailySchedule : ICompanyScopedEntity
{
    private ProfessionalDailySchedule()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public Guid ProfessionalAvailabilityId { get; private set; }

    public DayOfWeek Weekday { get; private set; }

    public TimeOnly EntryTime { get; private set; }

    public TimeOnly ExitTime { get; private set; }

    public TimeOnly? LunchStartTime { get; private set; }

    public TimeOnly? LunchEndTime { get; private set; }

    internal static ProfessionalDailySchedule Create(
        Guid companyId,
        Guid professionalAvailabilityId,
        ProfessionalDailyScheduleDefinition definition) =>
        new()
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            ProfessionalAvailabilityId = professionalAvailabilityId,
            Weekday = definition.Weekday,
            EntryTime = definition.EntryTime,
            ExitTime = definition.ExitTime,
            LunchStartTime = definition.LunchStartTime,
            LunchEndTime = definition.LunchEndTime
        };
}
