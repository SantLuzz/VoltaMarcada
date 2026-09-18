using Microsoft.Extensions.Options;

namespace VoltaMarcada.Web.Configuration;

public sealed class SupportOptions { public const string SectionName = "Support"; public string WhatsAppNumber { get; init; } = string.Empty; }
public interface ISupportConfiguration { string? WhatsAppUrl { get; } }
public sealed class SupportConfiguration(IOptions<SupportOptions> options) : ISupportConfiguration
{
    public string? WhatsAppUrl
    {
        get
        {
            var configuredNumber = options.Value.WhatsAppNumber.Trim();
            var digits = new string(configuredNumber.Where(char.IsDigit).ToArray());
            if (configuredNumber.StartsWith('+'))
            {
                return digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal) ? $"https://wa.me/{digits}" : null;
            }
            if (digits.Length is 10 or 11) digits = "55" + digits;

            return digits.Length is 12 or 13 && digits.StartsWith("55", StringComparison.Ordinal) ? $"https://wa.me/{digits}" : null;
        }
    }
}
