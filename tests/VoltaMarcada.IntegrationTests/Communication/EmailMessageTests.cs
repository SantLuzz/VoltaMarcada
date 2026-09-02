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
}
