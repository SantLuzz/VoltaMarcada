using Microsoft.Extensions.Options;
using VoltaMarcada.Application.Support;

namespace VoltaMarcada.Infrastructure.Communication;

public sealed class SmtpSupportRequestDeliveryConfiguration(IOptions<SmtpOptions> options) : ISupportRequestDeliveryConfiguration
{
    public string? RecipientEmail => string.IsNullOrWhiteSpace(options.Value.SenderEmail)
        ? null
        : options.Value.SenderEmail.Trim();
}
