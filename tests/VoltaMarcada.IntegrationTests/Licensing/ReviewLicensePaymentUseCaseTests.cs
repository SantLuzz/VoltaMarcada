using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class ReviewLicensePaymentUseCaseTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 5, 10, 15, 0, TimeSpan.Zero);

    [Fact]
    public async Task Approve_renews_the_payment_company_license_and_records_the_administrator()
    {
        var fixture = new ReviewFixture();

        var result = await fixture.ReviewAsync(LicensePaymentReviewDecision.Approve);

        Assert.True(result.IsSuccess);
        Assert.Equal(LicensePaymentStatus.Approved, fixture.Payment.Status);
        Assert.Equal(fixture.AdministratorId, fixture.Payment.DecidedByUserId);
        Assert.Equal(Now.AddMonths(1), fixture.License.PaidUntilUtc);
    }

    [Fact]
    public async Task Reject_requires_a_reason_and_does_not_renew_the_license()
    {
        var fixture = new ReviewFixture();

        var result = await fixture.ReviewAsync(LicensePaymentReviewDecision.Reject, "Proof is not legible.");

        Assert.True(result.IsSuccess);
        Assert.Equal(LicensePaymentStatus.Rejected, fixture.Payment.Status);
        Assert.Equal("Proof is not legible.", fixture.Payment.DecisionReason);
        Assert.Null(fixture.License.PaidUntilUtc);
    }

    private sealed class ReviewFixture
    {
        public ReviewFixture()
        {
            AdministratorId = Guid.NewGuid();
            var companyId = Guid.NewGuid();
            License = CompanyLicense.Create(companyId, Now, 14);
            Payment = LicensePayment.Create(companyId, 49.90m, Now);
            Payment.AttachProof($"pagamentos/{companyId}/{Payment.Id}.png", "image/png", 4, Now);
            Repository = new LicenseRepositoryStub(Payment, License);
        }

        public Guid AdministratorId { get; }

        public LicensePayment Payment { get; }

        public CompanyLicense License { get; }

        private LicenseRepositoryStub Repository { get; }

        public Task<OperationResult> ReviewAsync(LicensePaymentReviewDecision decision, string? reason = null) =>
            new ReviewLicensePaymentUseCase(
                Repository,
                new CurrentUserStub(AdministratorId),
                new FixedTimeProvider(Now))
            .ExecuteAsync(Payment.Id, new LicensePaymentReviewCommand(decision, reason));
    }

    private sealed class LicenseRepositoryStub(LicensePayment payment, CompanyLicense license) : ILicenseRepository
    {
        public Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult<CompanyLicenseMembership?>(null);

        public Task<CompanyLicense?> GetForCompanyAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(companyId == license.CompanyId ? license : null);

        public Task<bool> HasPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(payment.CompanyId == companyId && payment.Status == LicensePaymentStatus.PendingReview);

        public Task<LicensePayment?> GetPendingPaymentAsync(Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(payment.CompanyId == companyId && payment.Status == LicensePaymentStatus.PendingReview ? payment : null);

        public Task<LicensePayment?> GetPaymentAsync(Guid paymentId, Guid companyId, CancellationToken cancellationToken = default) =>
            Task.FromResult(payment.Id == paymentId && payment.CompanyId == companyId ? payment : null);

        public Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(LicensePayment value, CancellationToken cancellationToken = default) =>
            Task.FromResult(LicensePaymentPersistenceOutcome.Success);

        public Task<LicensePaymentReviewPersistenceOutcome> ReviewPaymentAsync(
            Guid paymentId,
            Guid administratorId,
            LicensePaymentReviewCommand command,
            DateTimeOffset now,
            CancellationToken cancellationToken = default)
        {
            if (payment.Id != paymentId || payment.Status != LicensePaymentStatus.PendingReview)
            {
                return Task.FromResult(LicensePaymentReviewPersistenceOutcome.NotPending);
            }

            if (command.Decision == LicensePaymentReviewDecision.Approve)
            {
                payment.Approve(administratorId, now);
                license.Renew(now);
            }
            else
            {
                payment.Reject(administratorId, command.Reason!, now);
            }

            return Task.FromResult(LicensePaymentReviewPersistenceOutcome.Success);
        }
    }

    private sealed class CurrentUserStub(Guid userId) : ICurrentUser
    {
        public bool IsAuthenticated => true;

        public Guid UserId => userId;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
