using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Professionals;

public sealed record SaveProfessionalAvailabilityCommand(
    ProfessionalWorkDays WorkDays,
    TimeOnly EntryTime,
    TimeOnly LunchStartTime,
    TimeOnly LunchEndTime,
    TimeOnly ExitTime);

public sealed record CreateScheduleBlockCommand(
    ScheduleBlockType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Notes);
