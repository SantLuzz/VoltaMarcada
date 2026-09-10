using System.Globalization;
using System.Text;
using VoltaMarcada.Application.Abstractions.Payments;

namespace VoltaMarcada.Infrastructure.Payments;

public sealed class BrCodePixPayloadGenerator : IPixPayloadGenerator
{
    private const string PixGui = "BR.GOV.BCB.PIX";

    public PixPaymentPayload Generate(PixPaymentRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (string.IsNullOrWhiteSpace(request.Key) || request.Amount <= 0)
        {
            throw new ArgumentException("PIX key and positive amount are required.", nameof(request));
        }

        var key = request.Key.Trim();
        var beneficiaryName = NormalizeMerchantText(request.BeneficiaryName, 25, nameof(request.BeneficiaryName));
        var beneficiaryCity = NormalizeMerchantText(request.BeneficiaryCity, 15, nameof(request.BeneficiaryCity));
        var merchantAccount = Field("00", PixGui) + Field("01", key);
        var additionalData = Field("05", "***");
        var amount = request.Amount.ToString("0.00", CultureInfo.InvariantCulture);

        var payloadWithoutCrc =
            Field("00", "01") +
            Field("01", "11") +
            Field("26", merchantAccount) +
            Field("52", "0000") +
            Field("53", "986") +
            Field("54", amount) +
            Field("58", "BR") +
            Field("59", beneficiaryName) +
            Field("60", beneficiaryCity) +
            Field("62", additionalData) +
            "6304";

        return new PixPaymentPayload(payloadWithoutCrc + BrCodeCrc16.Calculate(payloadWithoutCrc));
    }

    private static string Field(string id, string value)
    {
        var length = Encoding.UTF8.GetByteCount(value);
        if (length > 99)
        {
            throw new ArgumentException($"BR Code field {id} is too long.");
        }

        return id + length.ToString("D2", CultureInfo.InvariantCulture) + value;
    }

    private static string NormalizeMerchantText(string value, int maximumLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new ArgumentException("Beneficiary text is required.", parameterName);
        }

        var decomposed = value.Trim().Normalize(NormalizationForm.FormD);
        var normalized = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            var upper = char.ToUpperInvariant(character);
            normalized.Append(upper is >= 'A' and <= 'Z' or >= '0' and <= '9' or ' '
                ? upper
                : ' ');
        }

        var compact = string.Join(' ', normalized.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
        if (compact.Length == 0)
        {
            throw new ArgumentException("Beneficiary text is invalid.", parameterName);
        }

        return compact.Length <= maximumLength ? compact : compact[..maximumLength].TrimEnd();
    }
}

public static class BrCodeCrc16
{
    public static string Calculate(string value)
    {
        ArgumentNullException.ThrowIfNull(value);
        var crc = 0xFFFF;
        foreach (var valueByte in Encoding.UTF8.GetBytes(value))
        {
            crc ^= valueByte << 8;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0
                    ? ((crc << 1) ^ 0x1021) & 0xFFFF
                    : (crc << 1) & 0xFFFF;
            }
        }

        return crc.ToString("X4", CultureInfo.InvariantCulture);
    }
}
