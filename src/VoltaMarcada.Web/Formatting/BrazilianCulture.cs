using System.Globalization;

namespace VoltaMarcada.Web.Formatting;

public static class BrazilianCulture
{
    public static CultureInfo Value { get; } = CultureInfo.GetCultureInfo("pt-BR");

    public const string DateFormat = "dd/MM/yyyy";
}
