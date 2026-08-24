using ServiceSync.Application.Abstractions.Communication;

namespace ServiceSync.Infrastructure.Communication;

public sealed class WhatsAppLinkBuilder : IWhatsAppLinkBuilder
{
    public bool TryBuild(string phoneNumber, string message, out string link)
    {
        var normalizedPhoneNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
        if (normalizedPhoneNumber.Length is < 10 or > 15 || string.IsNullOrWhiteSpace(message))
        {
            link = string.Empty;
            return false;
        }

        if (normalizedPhoneNumber.Length is 10 or 11)
        {
            normalizedPhoneNumber = $"55{normalizedPhoneNumber}";
        }

        link = $"https://wa.me/{normalizedPhoneNumber}?text={Uri.EscapeDataString(message)}";
        return true;
    }
}
