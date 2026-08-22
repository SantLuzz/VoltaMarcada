namespace ServiceSync.Application.Scheduling;

public interface ISchedulingConfiguration
{
    int SlotIntervalMinutes { get; }

    string DefaultTimeZoneId { get; }

    IReadOnlySet<string> AllowedTimeZoneIds { get; }
}
