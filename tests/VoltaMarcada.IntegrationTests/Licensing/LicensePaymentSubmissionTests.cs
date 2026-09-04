using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Payments;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Infrastructure.Payments;
using VoltaMarcada.Infrastructure.Persistence;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class LicensePaymentSubmissionTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 4, 15, 30, 0, TimeSpan.Zero);

    [Fact]
    public async Task Submit_creates_pending_payment_and_company_scoped_proof()
    {
        var fixture = new PaymentFixture();

        var result = await fixture.SubmitPaymentAsync("proof.png", "image/png", ValidPng(), 1_024);

        Assert.True(result.IsSuccess);
        Assert.Equal(LicensePaymentStatus.PendingReview, result.Value!.Status);
        Assert.Equal(fixture.CompanyId, Assert.Single(fixture.Repository.Payments).CompanyId);
        Assert.Equal(49.90m, result.Value.Amount);
        Assert.Equal(Now, result.Value.SubmittedAtUtc);
        Assert.Equal($"pagamentos/{fixture.CompanyId}", fixture.Storage.LastUpload!.Folder);
        Assert.Equal($"{result.Value.Id}.png", fixture.Storage.LastUpload.FileName);
        Assert.Equal($"pagamentos/{fixture.CompanyId}/{result.Value.Id}.png", Assert.Single(fixture.Repository.Payments).ProofFileKey);
    }

    [Theory]
    [InlineData("proof.png", "image/png", 0)]
    [InlineData("proof.png", "image/png", 5_000_001)]
    [InlineData("proof.exe", "application/octet-stream", 1_024)]
    [InlineData("proof.png", "application/pdf", 1_024)]
    [InlineData("proof.jpeg", "image/png", 1_024)]
    public async Task Submit_rejects_invalid_size_type_or_extension_before_upload(
        string fileName,
        string contentType,
        long length)
    {
        var fixture = new PaymentFixture();

        var result = await fixture.SubmitPaymentAsync(fileName, contentType, ValidPng(), length);

        Assert.False(result.IsSuccess);
        Assert.Equal(LicenseErrors.InvalidPaymentProof, result.Error);
        Assert.Null(fixture.Storage.LastUpload);
        Assert.Empty(fixture.Repository.Payments);
    }

    [Theory]
    [InlineData("proof.pdf", "application/pdf")]
    [InlineData("proof.jpg", "image/jpeg")]
    [InlineData("proof.jpeg", "image/jpeg")]
    [InlineData("proof.png", "image/png")]
    [InlineData("proof.webp", "image/webp")]
    public async Task Submit_accepts_each_supported_extension_and_matching_content_type(
        string fileName,
        string contentType)
    {
        var fixture = new PaymentFixture();

        var result = await fixture.SubmitPaymentAsync(fileName, contentType, ValidPng(), 1_024);

        Assert.True(result.IsSuccess);
    }

    [Fact]
    public async Task Submit_returns_conflict_without_upload_when_pending_payment_exists()
    {
        var fixture = new PaymentFixture(hasPendingPayment: true);

        var result = await fixture.SubmitPaymentAsync("proof.pdf", "application/pdf", ValidPng(), 1_024);

        Assert.False(result.IsSuccess);
        Assert.Equal(LicenseErrors.PendingPaymentAlreadyExists, result.Error);
        Assert.Null(fixture.Storage.LastUpload);
    }

    [Fact]
    public async Task Submit_deletes_uploaded_proof_when_pending_unique_constraint_race_is_lost()
    {
        var fixture = new PaymentFixture(addOutcome: LicensePaymentPersistenceOutcome.PendingPaymentAlreadyExists);

        var result = await fixture.SubmitPaymentAsync("proof.webp", "image/webp", ValidPng(), 1_024);

        Assert.False(result.IsSuccess);
        Assert.Equal(LicenseErrors.PendingPaymentAlreadyExists, result.Error);
        Assert.Equal(fixture.Storage.LastUploadResult!.Key, Assert.Single(fixture.Storage.DeletedKeys));
    }

    [Fact]
    public async Task Submit_uses_current_company_instead_of_upload_input()
    {
        var fixture = new PaymentFixture();

        await fixture.SubmitPaymentAsync("proof.pdf", "application/pdf", ValidPng(), 1_024);

        var payment = Assert.Single(fixture.Repository.Payments);
        Assert.Equal(fixture.CompanyId, payment.CompanyId);
    }

    [Fact]
    public void Generate_returns_static_payload_with_valid_crc()
    {
        var generator = new BrCodePixPayloadGenerator();

        var payload = generator.Generate(
            new PixPaymentRequest("key@example.com", "VoltaMarcada", "SAO PAULO", 49.90m));

        Assert.StartsWith("000201010211", payload.CopyAndPasteCode, StringComparison.Ordinal);
        Assert.Contains("0014BR.GOV.BCB.PIX0115key@example.com", payload.CopyAndPasteCode, StringComparison.Ordinal);
        Assert.Contains("540549.90", payload.CopyAndPasteCode, StringComparison.Ordinal);
        Assert.EndsWith(
            "6304" + CalculateReferenceCrc(payload.CopyAndPasteCode[..^4]),
            payload.CopyAndPasteCode,
            StringComparison.Ordinal);
    }

    [Fact]
    public async Task Renewal_returns_configured_pix_and_current_pending_payment()
    {
        var fixture = new PaymentFixture();
        var submission = await fixture.SubmitPaymentAsync("proof.png", "image/png", ValidPng(), 1_024);
        Assert.True(submission.IsSuccess);

        var result = await fixture.GetRenewalAsync();

        Assert.True(result.IsSuccess);
        Assert.Equal(49.90m, result.Value!.MonthlyPrice);
        Assert.Contains("key@example.com", result.Value.Pix.CopyAndPasteCode, StringComparison.Ordinal);
        Assert.Equal(submission.Value!.Id, result.Value.PendingPayment!.Id);
    }

    [Fact]
    public void Ef_model_enforces_only_one_pending_payment_per_company()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        using var db = new AppDbContext(options);
        var entity = db.Model.FindEntityType(typeof(LicensePayment));

        var index = Assert.Single(entity!.GetIndexes(), candidate => candidate.IsUnique);
        Assert.Equal(nameof(LicensePayment.CompanyId), Assert.Single(index.Properties).Name);
        Assert.Equal("\"Status\" = 0", index.GetFilter());
    }

    private static MemoryStream ValidPng() => new([0x89, 0x50, 0x4E, 0x47]);

    private static string CalculateReferenceCrc(string value)
    {
        var crc = 0xFFFF;
        foreach (var valueByte in System.Text.Encoding.UTF8.GetBytes(value))
        {
            crc ^= valueByte << 8;
            for (var bit = 0; bit < 8; bit++)
            {
                crc = (crc & 0x8000) != 0
                    ? ((crc << 1) ^ 0x1021) & 0xFFFF
                    : (crc << 1) & 0xFFFF;
            }
        }

        return crc.ToString("X4", System.Globalization.CultureInfo.InvariantCulture);
    }

    private sealed class PaymentFixture
    {
        private readonly TestLicenseBillingConfiguration billing = new();

        public PaymentFixture(
            bool hasPendingPayment = false,
            LicensePaymentPersistenceOutcome addOutcome = LicensePaymentPersistenceOutcome.Success)
        {
            CompanyId = Guid.NewGuid();
            Repository = new LicenseRepositoryStub(hasPendingPayment, addOutcome);
            Storage = new FileStorageStub();
        }

        public Guid CompanyId { get; }

        public LicenseRepositoryStub Repository { get; }

        public FileStorageStub Storage { get; }

        public Task<OperationResult<LicensePaymentSubmissionDto>> SubmitPaymentAsync(
            string fileName,
            string contentType,
            Stream content,
            long length)
        {
            var useCase = new SubmitLicensePaymentUseCase(
                Repository,
                new CurrentCompanyStub(CompanyId),
                Storage,
                billing,
                new FixedTimeProvider(Now));

            return useCase.ExecuteAsync(
                new LicensePaymentProofUpload(new FileUpload(content, fileName, contentType, length)),
                CancellationToken.None);
        }

        public Task<OperationResult<LicenseRenewalDto>> GetRenewalAsync()
        {
            var useCase = new GetLicenseRenewalUseCase(
                Repository,
                new CurrentCompanyStub(CompanyId),
                billing,
                new BrCodePixPayloadGenerator());

            return useCase.ExecuteAsync(CancellationToken.None);
        }
    }

    private sealed class LicenseRepositoryStub(
        bool hasPendingPayment,
        LicensePaymentPersistenceOutcome addOutcome) : ILicenseRepository
    {
        public List<LicensePayment> Payments { get; } = [];

        public Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CompanyLicenseMembership?>(null);

        public Task<CompanyLicense?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CompanyLicense?>(null);

        public Task<bool> HasPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(hasPendingPayment || Payments.Any(payment =>
                payment.CompanyId == companyId && payment.Status == LicensePaymentStatus.PendingReview));

        public Task<LicensePayment?> GetPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(Payments.SingleOrDefault(payment =>
                payment.CompanyId == companyId && payment.Status == LicensePaymentStatus.PendingReview));

        public Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(
            LicensePayment payment,
            CancellationToken cancellationToken = default)
        {
            if (addOutcome == LicensePaymentPersistenceOutcome.Success)
            {
                Payments.Add(payment);
            }

            return Task.FromResult(addOutcome);
        }
    }

    private sealed class FileStorageStub : IFileStorage
    {
        public FileStorageUploadRequest? LastUpload { get; private set; }

        public FileStorageUploadResult? LastUploadResult { get; private set; }

        public List<string> DeletedKeys { get; } = [];

        public Task<FileStorageUploadResult> UploadAsync(
            FileStorageUploadRequest request,
            CancellationToken cancellationToken)
        {
            LastUpload = request;
            LastUploadResult = new FileStorageUploadResult($"{request.Folder}/{request.FileName}");
            return Task.FromResult(LastUploadResult);
        }

        public Task<FileStorageDownloadResult> GetAsync(string key, CancellationToken cancellationToken) =>
            throw new NotSupportedException();

        public Task DeleteAsync(string key, CancellationToken cancellationToken)
        {
            DeletedKeys.Add(key);
            return Task.CompletedTask;
        }
    }

    private sealed class CurrentCompanyStub(Guid companyId) : ICurrentCompany
    {
        public Task<Guid?> FindCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<Guid?>(companyId);

        public Task<Guid> GetCompanyIdAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(companyId);
    }

    private sealed class TestLicenseBillingConfiguration : ILicenseBillingConfiguration
    {
        public decimal MonthlyPrice => 49.90m;

        public string PixKey => "key@example.com";

        public string BeneficiaryName => "VoltaMarcada";

        public string BeneficiaryCity => "SAO PAULO";

        public long MaximumProofSizeBytes => 5_000_000;
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }
}
