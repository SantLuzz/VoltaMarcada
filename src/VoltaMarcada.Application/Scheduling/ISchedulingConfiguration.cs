namespace VoltaMarcada.Application.Scheduling;

public interface ISchedulingConfiguration
{
    int SlotIntervalMinutes { get; }

    int AvailabilitySearchDays { get; }

    int AvailabilityResultLimit { get; }

    string DefaultTimeZoneId { get; }

    IReadOnlySet<string> AllowedTimeZoneIds { get; }
}
