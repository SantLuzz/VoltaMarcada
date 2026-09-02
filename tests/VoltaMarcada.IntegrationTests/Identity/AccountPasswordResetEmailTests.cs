using VoltaMarcada.Application.Abstractions.Communication;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Identity;

public sealed class AccountPasswordResetEmailTests
{
    [Fact]
    public void Create_ShouldUseResetUrlWithoutExposingTheTokenInTheSubject()
    {
        var resetUrl = new Uri("https://volta.example/account/reset-password?token=sensitive-token");

        var email = AccountPasswordResetEmail.Create("Ana Silva", resetUrl);

        Assert.Equal("Redefina sua senha", email.Subject);
        Assert.Contains(resetUrl.AbsoluteUri, email.HtmlBody, StringComparison.Ordinal);
        Assert.DoesNotContain("sensitive-token", email.Subject, StringComparison.Ordinal);
    }
}
