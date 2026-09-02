using System.Globalization;

namespace VoltaMarcada.Web.Formatting;

public sealed class BrazilianDisplayFormatter : IBrazilianDisplayFormatter
{
    private static readonly CultureInfo BrazilianCulture = CultureInfo.GetCultureInfo("pt-BR");

    public string FormatDateTime(DateTimeOffset utcValue, TimeZoneInfo timeZone) =>
        TimeZoneInfo.ConvertTime(utcValue, timeZone).ToString("dd/MM/yyyy HH:mm", BrazilianCulture);

    public string FormatCurrency(decimal value) => value.ToString("C", BrazilianCulture);
}
