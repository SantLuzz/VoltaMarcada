namespace VoltaMarcada.Application.Abstractions.Communication;

public interface IAccountEmailSender
{
    Task SendPasswordResetAsync(
        string recipientEmail,
        string recipientName,
        Uri resetUrl,
        CancellationToken cancellationToken = default);
}

public sealed record AccountPasswordResetEmail(string Subject, string HtmlBody)
{
    public static AccountPasswordResetEmail Create(string recipientName, Uri resetUrl)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(recipientName);
        ArgumentNullException.ThrowIfNull(resetUrl);

        return new(
            "Redefina sua senha",
            $"<p>Olá, {System.Net.WebUtility.HtmlEncode(recipientName)}.</p><p>Para redefinir sua senha, <a href=\"{System.Net.WebUtility.HtmlEncode(resetUrl.AbsoluteUri)}\">acesse este link</a>.</p>");
    }
}
