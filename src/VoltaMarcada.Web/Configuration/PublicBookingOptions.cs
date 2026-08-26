using Microsoft.Extensions.Options;
using VoltaMarcada.Application.PublicBooking;

namespace VoltaMarcada.Web.Configuration;

public sealed class PublicBookingOptions
{
    public const string SectionName = "PublicBooking";

    public int MaintenanceLinkLifetimeDays { get; init; } = 30;
}

public sealed class PublicBookingConfiguration(IOptions<PublicBookingOptions> options) : IPublicBookingConfiguration
{
    public int MaintenanceLinkLifetimeDays => options.Value.MaintenanceLinkLifetimeDays;
}
