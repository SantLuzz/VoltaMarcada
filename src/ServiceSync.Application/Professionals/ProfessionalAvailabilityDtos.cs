using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Professionals;

public sealed record ProfessionalAvailabilityDto(
    Guid ProfessionalId,
    IReadOnlyList<DailyScheduleDto>? DailySchedules,
    IReadOnlyList<ScheduleBlockDto> ScheduleBlocks);

public sealed record DailyScheduleDto(
    DayOfWeek Weekday,
    TimeOnly EntryTime,
    TimeOnly ExitTime,
    TimeOnly? LunchStartTime,
    TimeOnly? LunchEndTime)
{
    internal static DailyScheduleDto FromEntity(ProfessionalDailySchedule schedule) =>
        new(
            schedule.Weekday,
            schedule.EntryTime,
            schedule.ExitTime,
            schedule.LunchStartTime,
            schedule.LunchEndTime);
}

public sealed record ScheduleBlockDto(
    Guid Id,
    ScheduleBlockType Type,
    DateOnly StartDate,
    DateOnly EndDate,
    bool IsFullDay,
    TimeOnly? StartTime,
    TimeOnly? EndTime,
    string? Notes)
{
    internal static ScheduleBlockDto FromEntity(ScheduleBlock block) =>
        new(
            block.Id,
            block.Type,
            block.StartDate,
            block.EndDate,
            block.IsFullDay,
            block.StartTime,
            block.EndTime,
            block.Notes);
}
