using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Support;
using VoltaMarcada.Domain.Entities;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Support;

public sealed class CreateSupportTicketUseCaseTests
{
    [Fact]
    public async Task ExecuteAsync_CreatesTicketAndStoresValidatedAttachment()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(new(
            "Agenda",
            "Não carrega",
            [new("capture.png", "application/octet-stream", [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]) ]));

        Assert.True(result.IsSuccess);
        Assert.Equal(1, result.Value!.Number);
        var ticket = Assert.Single(fixture.Repository.Tickets);
        Assert.Equal("member@company.com", ticket.RequesterEmail);
        Assert.Equal("Empresa teste", ticket.CompanyTradeName);
        var attachment = Assert.Single(Assert.Single(ticket.Messages).Attachments);
        Assert.Equal("image/png", attachment.ContentType);
        Assert.Single(fixture.FileStorage.Uploads);
    }

    [Fact]
    public async Task ExecuteAsync_RejectsContentThatOnlyPretendsToBePng()
    {
        var fixture = new Fixture();

        var result = await fixture.UseCase.ExecuteAsync(new("Ajuda", "Arquivo inválido", [new("photo.png", "image/png", "not image"u8.ToArray())]));

        Assert.False(result.IsSuccess);
        Assert.Empty(fixture.Repository.Tickets);
        Assert.Empty(fixture.FileStorage.Uploads);
    }

    private sealed class Fixture
    {
        private readonly Guid userId = Guid.NewGuid();
        private readonly Guid companyId = Guid.NewGuid();

        public Fixture()
        {
            Repository = new SupportTicketRepositoryStub();
            FileStorage = new FileStorageStub();
            UseCase = new CreateSupportTicketUseCase(
                new CurrentUserStub(userId),
                new CurrentCompanyStub(companyId),
                new SupportRequestContextReaderStub(new("Member", "member@company.com", "Empresa teste", "Empresa teste LTDA")),
                Repository,
                FileStorage,
                new FixedTimeProvider());
        }

        public SupportTicketRepositoryStub Repository { get; }
        public FileStorageStub FileStorage { get; }
        public CreateSupportTicketUseCase UseCase { get; }
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

    public sealed class SupportTicketRepositoryStub : ISupportTicketRepository
    {
        public List<SupportTicket> Tickets { get; } = [];
        public Task<ISupportTicketTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default) => Task.FromResult<ISupportTicketTransaction>(new TransactionStub());
        public Task<long> GetNextNumberAsync(CancellationToken cancellationToken = default) => Task.FromResult((long)Tickets.Count + 1);
        public Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default) { Tickets.Add(ticket); return Task.CompletedTask; }
        public Task<IReadOnlyList<PlatformSupportTicketListItem>> GetAllAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<PlatformSupportTicketListItem>>([]);
        public Task<SupportTicket?> GetForUpdateAsync(Guid ticketId, CancellationToken cancellationToken = default) => Task.FromResult<SupportTicket?>(Tickets.SingleOrDefault(ticket => ticket.Id == ticketId));
        public Task<SupportTicketAttachment?> GetAttachmentAsync(Guid attachmentId, CancellationToken cancellationToken = default) => Task.FromResult<SupportTicketAttachment?>(null);
        public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
    }

    public sealed class FileStorageStub : IFileStorage
    {
        public List<FileStorageUploadRequest> Uploads { get; } = [];
        public Task<FileStorageUploadResult> UploadAsync(FileStorageUploadRequest request, CancellationToken cancellationToken) { Uploads.Add(request); return Task.FromResult(new FileStorageUploadResult("support/test/capture")); }
        public Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken) => throw new NotImplementedException();
        public Task DeleteAsync(string key, CancellationToken cancellationToken) => Task.CompletedTask;
    }

    private sealed class TransactionStub : ISupportTicketTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => new(2026, 9, 21, 12, 0, 0, TimeSpan.Zero);
    }
}