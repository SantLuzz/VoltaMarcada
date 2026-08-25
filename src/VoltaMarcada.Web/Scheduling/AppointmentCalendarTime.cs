namespace VoltaMarcada.Web.Scheduling;

public static class AppointmentCalendarTime
{
    public static DateTime ToLocalDateTime(DateTimeOffset value, string timeZoneId) =>
        TimeZoneInfo.ConvertTime(value, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId)).DateTime;

    public static (DateTimeOffset StartAtUtc, DateTimeOffset EndAtUtc) GetUtcRange(
        DateOnly startDate,
        DateOnly endDateExclusive,
        IEnumerable<string> timeZoneIds)
    {
        var ranges = timeZoneIds.Select(timeZoneId =>
        {
            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
            return (
                new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(startDate.ToDateTime(TimeOnly.MinValue), timeZone)),
                new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(endDateExclusive.ToDateTime(TimeOnly.MinValue), timeZone)));
        }).ToArray();

        return (ranges.Min(range => range.Item1), ranges.Max(range => range.Item2));
    }
}
