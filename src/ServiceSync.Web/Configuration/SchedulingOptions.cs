using ServiceSync.Application.Scheduling;
using Microsoft.Extensions.Options;

namespace ServiceSync.Web.Configuration;

public sealed class SchedulingOptions
{
    public const string SectionName = "Scheduling";

    public int SlotIntervalMinutes { get; init; } = 30;

    public string DefaultTimeZoneId { get; init; } = "America/Sao_Paulo";

    public IReadOnlyList<string> AllowedTimeZoneIds { get; init; } = ["America/Sao_Paulo", "America/Campo_Grande"];
}

public sealed class SchedulingConfiguration(IOptions<SchedulingOptions> options) : ISchedulingConfiguration
{
    private readonly SchedulingOptions value = options.Value;

    public int SlotIntervalMinutes => value.SlotIntervalMinutes;

    public string DefaultTimeZoneId => value.DefaultTimeZoneId;

    public IReadOnlySet<string> AllowedTimeZoneIds => value.AllowedTimeZoneIds.ToHashSet(StringComparer.Ordinal);
}
