namespace VoltaMarcada.Web.Formatting;

public interface IBrazilianDisplayFormatter
{
    string FormatDateTime(DateTimeOffset utcValue, TimeZoneInfo timeZone);

    string FormatCurrency(decimal value);
}
