using System.Net;
using System.Net.Mail;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using VoltaMarcada.Application.Abstractions.Communication;

namespace VoltaMarcada.Infrastructure.Communication;

public sealed class SmtpEmailSender(IOptions<SmtpOptions> options, ILogger<SmtpEmailSender> logger) : IEmailSender
{
    public async Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
    {
        var smtpOptions = options.Value;
        if (!smtpOptions.Enabled)
        {
            logger.LogWarning("Email delivery is disabled for recipient {RecipientEmail}", email.RecipientEmail);
            return;
        }

        try
        {
            using var message = new MailMessage(new MailAddress(smtpOptions.SenderEmail, smtpOptions.SenderName), new MailAddress(email.RecipientEmail))
            {
                Subject = email.Subject,
                Body = email.HtmlBody,
                IsBodyHtml = true
            };
            using var client = new SmtpClient(smtpOptions.Host, smtpOptions.Port)
            {
                EnableSsl = smtpOptions.UseTls,
                UseDefaultCredentials = false,
                Credentials = string.IsNullOrWhiteSpace(smtpOptions.Username)
                    ? CredentialCache.DefaultNetworkCredentials
                    : new NetworkCredential(smtpOptions.Username, smtpOptions.Password)
            };
            await client.SendMailAsync(message, cancellationToken);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Unable to deliver email to recipient {RecipientEmail}", email.RecipientEmail);
        }
    }
}
