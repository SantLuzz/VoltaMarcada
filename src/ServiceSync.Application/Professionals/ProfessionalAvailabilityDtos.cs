using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Application.Professionals;

public sealed record ProfessionalAvailabilityDto(
    Guid ProfessionalId,
    WeeklyScheduleDto? WeeklySchedule,
    IReadOnlyList<ScheduleBlockDto> ScheduleBlocks);

public sealed record WeeklyScheduleDto(
    ProfessionalWorkDays WorkDays,
    TimeOnly EntryTime,
    TimeOnly LunchStartTime,
    TimeOnly LunchEndTime,
    TimeOnly ExitTime)
{
    internal static WeeklyScheduleDto FromEntity(ProfessionalAvailability availability) =>
        new(
            availability.WorkDays,
            availability.EntryTime,
            availability.LunchStartTime,
            availability.LunchEndTime,
            availability.ExitTime);
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
