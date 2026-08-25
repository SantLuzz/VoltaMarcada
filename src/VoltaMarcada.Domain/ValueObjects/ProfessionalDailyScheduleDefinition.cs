namespace VoltaMarcada.Domain.ValueObjects;

public sealed record ProfessionalDailyScheduleDefinition(
    DayOfWeek Weekday,
    TimeOnly EntryTime,
    TimeOnly ExitTime,
    TimeOnly? LunchStartTime,
    TimeOnly? LunchEndTime);
