using VoltaMarcada.Application.Abstractions.Communication;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Support;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class SendSupportRequestUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_UsesStoredIdentityAndReplyTo()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(new(
            "Agenda",
            "N?o carrega",
            [new("capture.png", "application/octet-stream", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]) ]));

        Assert.True(result.IsSuccess);
        var email = Assert.Single(fixture.EmailSender.Messages);
        Assert.Equal("support@voltamarcada.com", email.RecipientEmail);
        Assert.Equal("member@company.com", email.ReplyToEmail);
        Assert.Contains("Empresa teste", email.HtmlBody);
        Assert.Equal("image/png", Assert.Single(email.Attachments).ContentType);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsContentThatOnlyPretendsToBePng()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(new("Ajuda", "Arquivo inv?lido", [new("photo.png", "image/png", "not image"u8.ToArray())]));

        Assert.False(result.IsSuccess);
        Assert.Empty(fixture.EmailSender.Messages);
    }

    private sealed class Fixture
    {
        private readonly Guid userId = Guid.NewGuid();
        private readonly Guid companyId = Guid.NewGuid();

        public Fixture()
        {
            EmailSender = new EmailSenderStub();
            UseCase = new SendSupportRequestUseCase(
                new CurrentUserStub(userId),
                new CurrentCompanyStub(companyId),
                new SupportRequestContextReaderStub(new("Member", "member@company.com", "Empresa teste", "Empresa teste LTDA")),
                new SupportRequestDeliveryConfigurationStub("support@voltamarcada.com"),
                EmailSender);
        }

        public EmailSenderStub EmailSender { get; }
        public SendSupportRequestUseCase UseCase { get; }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;
        public Guid UserId { get; } = userId;
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult<Guid?>(companyId);
        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) => Task.FromResult(companyId);
    }

    private sealed class SupportRequestContextReaderStub(SupportRequestContext? context) : ISupportRequestContextReader
    {
        public Task<SupportRequestContext?> GetAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default) => Task.FromResult(context);
    }

    private sealed class SupportRequestDeliveryConfigurationStub(string recipientEmail) : ISupportRequestDeliveryConfiguration
    {
        public string? RecipientEmail { get; } = recipientEmail;
    }

    public sealed class EmailSenderStub : IEmailSender
    {
        public List<EmailMessage> Messages { get; } = [];
        public Task SendAsync(EmailMessage email, CancellationToken cancellationToken = default) { Messages.Add(email); return Task.CompletedTask; }
    }
}
