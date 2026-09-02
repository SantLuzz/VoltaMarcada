using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Infrastructure.Communication;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Communication;

public sealed class PublicBookingNotificationSenderTests
{
    [Fact]
    public async Task SendNewAppointmentRequestAsync_ShouldExcludeLineBreaksFromTheSubject()
    {
        var emailSender = new CapturingEmailSender();
        var sender = new PublicBookingNotificationSender(emailSender);

        await sender.SendNewAppointmentRequestAsync(
            "company@example.com",
            "VoltaMarcada\r\nBcc: another@example.com",
            "Ana Silva",
            "Maintenance",
            new DateOnly(2026, 9, 2),
            new TimeOnly(9, 0),
            "https://volta.example/solicitacoes/123",
            CancellationToken.None);

        Assert.NotNull(emailSender.Email);
        Assert.DoesNotContain('\r', emailSender.Email!.Subject);
        Assert.DoesNotContain('\n', emailSender.Email.Subject);
    }

    private sealed class CapturingEmailSender : IEmailSender
    {
        public EmailMessage? Email { get; private set; }

        public Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default)
        {
            Email = email;
            return Task.CompletedTask;
        }
    }
}
