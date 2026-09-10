using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class LicensePaymentReviewTests
{
    private static readonly DateTimeOffset SubmittedAtUtc = new(2026, 9, 4, 15, 30, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset DecidedAtUtc = new(2026, 9, 5, 10, 15, 0, TimeSpan.Zero);

    [Fact]
    public void Approve_pending_payment_records_auditable_decision()
    {
        var administratorId = Guid.NewGuid();
        var payment = CreatePendingPayment();

        payment.Approve(administratorId, DecidedAtUtc);

        Assert.Equal(LicensePaymentStatus.Approved, payment.Status);
        Assert.Equal(administratorId, payment.DecidedByUserId);
        Assert.Equal(DecidedAtUtc, payment.DecidedAtUtc);
        Assert.Null(payment.DecisionReason);
    }

    [Fact]
    public void Reject_pending_payment_requires_and_records_a_reason()
    {
        var administratorId = Guid.NewGuid();
        var payment = CreatePendingPayment();

        payment.Reject(administratorId, "PIX amount does not match the plan.", DecidedAtUtc);

        Assert.Equal(LicensePaymentStatus.Rejected, payment.Status);
        Assert.Equal(administratorId, payment.DecidedByUserId);
        Assert.Equal(DecidedAtUtc, payment.DecidedAtUtc);
        Assert.Equal("PIX amount does not match the plan.", payment.DecisionReason);
        Assert.Throws<DomainException>(() => CreatePendingPayment().Reject(administratorId, " ", DecidedAtUtc));
    }

    private static LicensePayment CreatePendingPayment()
    {
        var payment = LicensePayment.Create(Guid.NewGuid(), 49.90m, SubmittedAtUtc);
        payment.AttachProof($"pagamentos/{payment.CompanyId}/{payment.Id}.png", "image/png", 4, SubmittedAtUtc);
        return payment;
    }
}
