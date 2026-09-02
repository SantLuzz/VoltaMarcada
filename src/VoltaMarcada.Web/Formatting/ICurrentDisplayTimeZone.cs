namespace VoltaMarcada.Web.Formatting;

public interface ICurrentDisplayTimeZone
{
    Task<TimeZoneInfo> GetTimeZoneAsync(CancellationToken cancellationToken = default);
}
