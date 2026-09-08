using VoltaMarcada.Domain.Abstractions.Tenancy;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class LicensePayment : ICompanyScopedEntity
{
    private LicensePayment()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public decimal Amount { get; private set; }

    public LicensePaymentStatus Status { get; private set; }

    public string? ProofFileKey { get; private set; }

    public string? ProofContentType { get; private set; }

    public long? ProofLength { get; private set; }

    public DateTimeOffset SubmittedAtUtc { get; private set; }

    public string? DecisionReason { get; private set; }

    public DateTimeOffset? DecidedAtUtc { get; private set; }

    public Guid? DecidedByUserId { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static LicensePayment Create(Guid companyId, decimal amount, DateTimeOffset now)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (amount <= 0)
        {
            throw new DomainException("Payment amount must be greater than zero.");
        }

        var utcNow = now.ToUniversalTime();
        return new LicensePayment
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Amount = amount,
            Status = LicensePaymentStatus.PendingReview,
            SubmittedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public void AttachProof(string fileKey, string contentType, long length, DateTimeOffset now)
    {
        if (Status != LicensePaymentStatus.PendingReview)
        {
            throw new DomainException("Only a pending payment can receive a proof.");
        }

        if (string.IsNullOrWhiteSpace(fileKey) || fileKey.Trim().Length > 512)
        {
            throw new DomainException("Payment proof file key is invalid.");
        }

        if (string.IsNullOrWhiteSpace(contentType) || contentType.Trim().Length > 100)
        {
            throw new DomainException("Payment proof content type is invalid.");
        }

        if (length <= 0)
        {
            throw new DomainException("Payment proof length must be greater than zero.");
        }

        ProofFileKey = fileKey.Trim();
        ProofContentType = contentType.Trim().ToLowerInvariant();
        ProofLength = length;
        UpdatedAtUtc = now.ToUniversalTime();
    }

    public void Approve(Guid administratorId, DateTimeOffset decidedAtUtc)
    {
        RecordDecision(LicensePaymentStatus.Approved, administratorId, null, decidedAtUtc);
    }

    public void Reject(Guid administratorId, string reason, DateTimeOffset decidedAtUtc)
    {
        if (string.IsNullOrWhiteSpace(reason) || reason.Trim().Length > 2_000)
        {
            throw new DomainException("Payment rejection reason is required and cannot exceed 2000 characters.");
        }

        RecordDecision(LicensePaymentStatus.Rejected, administratorId, reason.Trim(), decidedAtUtc);
    }

    private void RecordDecision(
        LicensePaymentStatus status,
        Guid administratorId,
        string? reason,
        DateTimeOffset decidedAtUtc)
    {
        if (Status != LicensePaymentStatus.PendingReview)
        {
            throw new DomainException("Only a pending payment can be reviewed.");
        }

        if (administratorId == Guid.Empty)
        {
            throw new DomainException("Platform administrator ID is required.");
        }

        var utcDecidedAt = decidedAtUtc.ToUniversalTime();
        Status = status;
        DecisionReason = reason;
        DecidedAtUtc = utcDecidedAt;
        DecidedByUserId = administratorId;
        UpdatedAtUtc = utcDecidedAt;
    }
}
