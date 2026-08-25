namespace VoltaMarcada.Domain.ValueObjects;

public static class BrazilianPostalCode
{
    public static bool TryNormalize(string? value, out string normalizedPostalCode)
    {
        normalizedPostalCode = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = value.Where(character => character is >= '0' and <= '9').ToArray();
        if (digits.Length != 8)
        {
            return false;
        }

        normalizedPostalCode = new string(digits);
        return true;
    }
}
