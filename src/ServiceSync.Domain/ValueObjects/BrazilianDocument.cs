namespace ServiceSync.Domain.ValueObjects;

public static class BrazilianDocument
{
    public static bool TryNormalize(string? value, out string normalizedDocument)
    {
        normalizedDocument = string.Empty;

        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var digits = new char[value.Length];
        var digitCount = 0;

        foreach (var character in value)
        {
            if (character is >= '0' and <= '9')
            {
                digits[digitCount++] = character;
            }
        }

        if (digitCount is not 11 and not 14 || HasRepeatedDigits(digits, digitCount))
        {
            return false;
        }

        var candidate = new string(digits, 0, digitCount);
        if (!IsValidCpf(candidate) && !IsValidCnpj(candidate))
        {
            return false;
        }

        normalizedDocument = candidate;
        return true;
    }

    private static bool HasRepeatedDigits(char[] digits, int length)
    {
        for (var index = 1; index < length; index++)
        {
            if (digits[index] != digits[0])
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsValidCpf(string value)
    {
        if (value.Length != 11)
        {
            return false;
        }

        return CalculateCheckDigit(value, 9, 10) == ToDigit(value[9])
            && CalculateCheckDigit(value, 10, 11) == ToDigit(value[10]);
    }

    private static bool IsValidCnpj(string value)
    {
        if (value.Length != 14)
        {
            return false;
        }

        return CalculateCnpjCheckDigit(value, 12) == ToDigit(value[12])
            && CalculateCnpjCheckDigit(value, 13) == ToDigit(value[13]);
    }

    private static int CalculateCheckDigit(string value, int count, int initialWeight)
    {
        var sum = 0;
        for (var index = 0; index < count; index++)
        {
            sum += ToDigit(value[index]) * (initialWeight - index);
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int CalculateCnpjCheckDigit(string value, int count)
    {
        var sum = 0;
        var weight = count == 12 ? 5 : 6;

        for (var index = 0; index < count; index++)
        {
            sum += ToDigit(value[index]) * weight;
            weight = weight == 2 ? 9 : weight - 1;
        }

        var remainder = sum % 11;
        return remainder < 2 ? 0 : 11 - remainder;
    }

    private static int ToDigit(char value) => value - '0';
}
