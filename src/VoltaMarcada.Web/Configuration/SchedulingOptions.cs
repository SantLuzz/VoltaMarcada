using VoltaMarcada.Application.Scheduling;
using Microsoft.Extensions.Options;

namespace VoltaMarcada.Web.Configuration;

public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    public int SlotIntervalMinutes { get; init; } = 30;

    public int AvailabilitySearchDays { get; init; } = 7;

    public int AvailabilityResultLimit { get; init; } = 10;

    public string DefaultTimeZoneId { get; init; } = "America/Sao_Paulo";

    public IReadOnlyList<string> AllowedTimeZoneIds { get; init; } = ["America/Sao_Paulo", "America/Campo_Grande"];
}

public sealed class SchedulingConfiguration(IOptions<SchedulingOptions> options) : ISchedulingConfiguration
{
    private readonly SchedulingOptions value = options.Value;

    public int SlotIntervalMinutes => value.SlotIntervalMinutes;

    public int AvailabilitySearchDays => value.AvailabilitySearchDays;

    public int AvailabilityResultLimit => value.AvailabilityResultLimit;

    public string DefaultTimeZoneId => value.DefaultTimeZoneId;

    public IReadOnlySet<string> AllowedTimeZoneIds => value.AllowedTimeZoneIds.ToHashSet(StringComparer.Ordinal);
}
