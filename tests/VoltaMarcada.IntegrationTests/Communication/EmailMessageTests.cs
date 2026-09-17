using VoltaMarcada.Application.Abstractions.Communication;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Communication;

public sealed class EmailMessageTests
{
    [Fact]
    public void Create_ShouldPreserveRecipientAndContent()
    {
        var email = new EmailMessage("company@example.com", "New request", "<p>Request content</p>");

        Assert.Equal("company@example.com", email.RecipientEmail);
        Assert.Equal("New request", email.Subject);
        Assert.Equal("<p>Request content</p>", email.HtmlBody);
    }


    [Fact]
    public void Create_ShouldSanitizeHeadersAndAttachmentFileName()
    {
        var email = new EmailMessage("company@example.com\r\nBcc: attacker@example.com", "New request\r\nBcc: attacker@example.com", "<p>Request content</p>", "support@example.com\r\nBcc: attacker@example.com", [new EmailAttachment("invoice\r\n.pdf", "application/pdf", [1, 2, 3])]);

        Assert.Equal("company@example.comBcc: attacker@example.com", email.RecipientEmail);
        Assert.Equal("New requestBcc: attacker@example.com", email.Subject);
        Assert.Equal("support@example.comBcc: attacker@example.com", email.ReplyToEmail);
        var attachment = Assert.Single(email.Attachments);
        Assert.Equal("invoice.pdf", attachment.FileName);
        Assert.Equal([1, 2, 3], attachment.Content);
}
}
