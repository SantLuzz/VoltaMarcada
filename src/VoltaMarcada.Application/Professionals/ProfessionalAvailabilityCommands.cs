using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Professionals;

public sealed record SaveProfessionalAvailabilityCommand(
    IReadOnlyList<SaveProfessionalDailyScheduleCommand> DailySchedules);

public sealed record SaveProfessionalDailyScheduleCommand(
    DayOfWeek Weekday,
    TimeOnly EntryTime,
    TimeOnly ExitTime,
    TimeOnly? LunchStartTime,
    TimeOnly? LunchEndTime);

public sealed record CreateScheduleBlockCommand(
    ScheduleBlockType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Notes);
