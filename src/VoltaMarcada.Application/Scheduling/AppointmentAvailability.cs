using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Scheduling;

public sealed class AppointmentAvailability(IProfessionalAvailabilityRepository availabilityRepository, IAppointmentRepository appointmentRepository)
{
    public async Task<bool> IsAvailableAsync(Professional professional, DateOnly date, TimeOnly startTime, int durationMinutes, Guid? excludedAppointmentId = null, CancellationToken cancellationToken = default)
    {
        if (durationMinutes <= 0 || durationMinutes > 24 * 60)
        {
            return false;
        }

        var endTime = startTime.AddMinutes(durationMinutes);
        if (endTime < startTime)
        {
            return false;
        }

        var availability = await availabilityRepository.GetAvailabilityAsync(professional.Id, professional.CompanyId, false, cancellationToken);
        var dailySchedule = availability?.DailySchedules.SingleOrDefault(schedule => schedule.Weekday == date.DayOfWeek);
        if (dailySchedule is null || startTime < dailySchedule.EntryTime || endTime > dailySchedule.ExitTime)
        {
            return false;
        }

        if (dailySchedule.LunchStartTime is { } lunchStart && dailySchedule.LunchEndTime is { } lunchEnd && startTime < lunchEnd && endTime > lunchStart)
        {
            return false;
        }

        var blocks = await availabilityRepository.GetScheduleBlocksAsync(professional.Id, professional.CompanyId, cancellationToken);
        if (blocks.Any(block => IsBlocked(block, date, startTime, endTime)))
        {
            return false;
        }

        if (!TryGetUtcInterval(professional.TimeZoneId, date, startTime, durationMinutes, out var startAtUtc, out var endAtUtc))
        {
            return false;
        }

        return !await appointmentRepository.HasAppointmentConflictAsync(professional.CompanyId, professional.Id, startAtUtc, endAtUtc, excludedAppointmentId, cancellationToken);
    }

    public static bool TryGetUtcInterval(string timeZoneId, DateOnly date, TimeOnly startTime, int durationMinutes, out DateTimeOffset startAtUtc, out DateTimeOffset endAtUtc)
    {
        startAtUtc = default;
        endAtUtc = default;
        if (durationMinutes <= 0)
        {
            return false;
        }

        try
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            var localStart = DateTime.SpecifyKind(date.ToDateTime(startTime), DateTimeKind.Unspecified);
            var localEnd = localStart.AddMinutes(durationMinutes);
            if (timeZone.IsInvalidTime(localStart) || timeZone.IsInvalidTime(localEnd))
            {
                return false;
            }

            startAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, timeZone));
            endAtUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localEnd, timeZone));
            return endAtUtc > startAtUtc;
        }
        catch (TimeZoneNotFoundException)
        {
            return false;
        }
        catch (InvalidTimeZoneException)
        {
            return false;
        }
    }

    private static bool IsBlocked(ScheduleBlock block, DateOnly date, TimeOnly startTime, TimeOnly endTime) =>
        date >= block.StartDate && date <= block.EndDate &&
        (block.IsFullDay || (block.StartDate == date && block.StartTime is { } blockStart && block.EndTime is { } blockEnd && startTime < blockEnd && endTime > blockStart));
}
