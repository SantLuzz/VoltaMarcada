namespace VoltaMarcada.Application.Abstractions.Communication;

public sealed record EmailAttachment
{
    public EmailAttachment(string fileName, string contentType, byte[] content)
    {
        FileName = Sanitize(fileName);
        ContentType = Sanitize(contentType);
        Content = content;
    }

    public string FileName { get; }
    public string ContentType { get; }
    public byte[] Content { get; }

    private static string Sanitize(string value) => value.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
}

public sealed record EmailDeliveryResult(bool IsSuccess, string? Error)
{
    public static EmailDeliveryResult Success() => new(true, null);
    public static EmailDeliveryResult Failure(string error) => new(false, error);
}

public interface IEmailSender
{
    Task<EmailDeliveryResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default);
}

public sealed record EmailMessage
{
    public EmailMessage(string recipientEmail, string subject, string htmlBody, string? replyToEmail = null, IEnumerable<EmailAttachment>? attachments = null)
    {
        RecipientEmail = Sanitize(recipientEmail);
        Subject = Sanitize(subject);
        HtmlBody = htmlBody;
        ReplyToEmail = string.IsNullOrWhiteSpace(replyToEmail) ? null : Sanitize(replyToEmail);
        Attachments = Array.AsReadOnly((attachments ?? []).ToArray());
    }

    public string RecipientEmail { get; }
    public string Subject { get; }
    public string HtmlBody { get; }
    public string? ReplyToEmail { get; }
    public IReadOnlyList<EmailAttachment> Attachments { get; }

    private static string Sanitize(string value) => value.Replace("\r", string.Empty, StringComparison.Ordinal).Replace("\n", string.Empty, StringComparison.Ordinal);
}

public sealed record AccountPasswordResetEmail(string Subject, string HtmlBody)
{
    public static AccountPasswordResetEmail Create(string recipientName, Uri resetUrl)
    {
        var safeName = System.Net.WebUtility.HtmlEncode(recipientName);
        var safeUrl = System.Net.WebUtility.HtmlEncode(resetUrl.AbsoluteUri);
        return new AccountPasswordResetEmail("Redefina sua senha", $"<p>Ol�, {safeName}.</p><p>Recebemos uma solicita��o para redefinir sua senha.</p><p><a href=\"{safeUrl}\">Redefinir senha</a></p><p>Se voc� n�o solicitou esta altera��o, ignore este e-mail.</p>");
    }
}
