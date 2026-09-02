namespace VoltaMarcada.Application.Abstractions.Communication;

public interface IEmailSender
{
    Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage
{
    public EmailMessage(string recipientEmail, string subject, string htmlBody)
    {
        RecipientEmail = recipientEmail;
        Subject = subject.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
        HtmlBody = htmlBody;
    }

    public string RecipientEmail { get; }

    public string Subject { get; }

    public string HtmlBody { get; }
}

public sealed record AccountPasswordResetEmail(string Subject, string HtmlBody)
{
    public static AccountPasswordResetEmail Create(string recipientName, Uri resetUrl)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(recipientName);
        var safeUrl = System.Net.WebUtility.HtmlEncode(resetUrl.AbsoluteUri);
        return new AccountPasswordResetEmail(
            "Redefina sua senha",
            $"<p>Ol\u00e1, {safeName}.</p><p>Recebemos uma solicita\u00e7\u00e3o para redefinir sua senha.</p><p><a href=\"{safeUrl}\">Redefinir senha</a></p><p>Se voc\u00ea n\u00e3o solicitou esta altera\u00e7\u00e3o, ignore este e-mail.</p>");
    }
}
