using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;

namespace VoltaMarcada.Domain.Entities;

public sealed class CompanyLicense
{
    private CompanyLicense()
    {
    }

    public Guid Id { get; private set; }

    public Guid CompanyId { get; private set; }

    public CompanyLicenseStatus Status { get; private set; }

    public DateTimeOffset TrialStartedAtUtc { get; private set; }

    public DateTimeOffset TrialEndsAtUtc { get; private set; }

    public DateTimeOffset? PaidUntilUtc { get; private set; }

    public DateTimeOffset CreatedAtUtc { get; private set; }

    public DateTimeOffset UpdatedAtUtc { get; private set; }

    public static CompanyLicense Create(Guid companyId, DateTimeOffset now, int trialDays)
    {
        if (companyId == Guid.Empty)
        {
            throw new DomainException("Company ID is required.");
        }

        if (trialDays <= 0)
        {
            throw new DomainException("Trial days must be greater than zero.");
        }

        var utcNow = now.ToUniversalTime();
        return new CompanyLicense
        {
            Id = Guid.NewGuid(),
            CompanyId = companyId,
            Status = CompanyLicenseStatus.Trial,
            TrialStartedAtUtc = utcNow,
            TrialEndsAtUtc = utcNow.AddDays(trialDays),
            CreatedAtUtc = utcNow,
            UpdatedAtUtc = utcNow
        };
    }

    public CompanyLicenseAccessMode GetAccessMode(DateTimeOffset now, LicenseLifecyclePolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var utcNow = now.ToUniversalTime();
        return PaidUntilUtc is { } paidUntilUtc
            ? GetPaidAccessMode(utcNow, paidUntilUtc, policy)
            : GetTrialAccessMode(utcNow, policy);
    }

    public void Renew(DateTimeOffset approvedAtUtc)
    {
        var utcApprovedAt = approvedAtUtc.ToUniversalTime();
        var renewalStartAt = PaidUntilUtc is { } paidUntilUtc && paidUntilUtc > utcApprovedAt
            ? paidUntilUtc
            : utcApprovedAt;

        PaidUntilUtc = renewalStartAt.AddMonths(1);
        Status = CompanyLicenseStatus.Paid;
        UpdatedAtUtc = utcApprovedAt;
    }

    private CompanyLicenseAccessMode GetTrialAccessMode(DateTimeOffset now, LicenseLifecyclePolicy policy)
    {
        if (now >= TrialEndsAtUtc.AddDays(policy.TrialTerminationDays))
        {
            return CompanyLicenseAccessMode.Terminated;
        }

        return now >= TrialEndsAtUtc.AddDays(policy.TrialToleranceDays)
            ? CompanyLicenseAccessMode.OwnerReadOnly
            : CompanyLicenseAccessMode.Operational;
    }

    private static CompanyLicenseAccessMode GetPaidAccessMode(
        DateTimeOffset now,
        DateTimeOffset paidUntilUtc,
        LicenseLifecyclePolicy policy)
    {
        if (now >= paidUntilUtc.AddDays(policy.PaidTerminationDays))
        {
            return CompanyLicenseAccessMode.Terminated;
        }

        return now >= paidUntilUtc.AddDays(policy.PaidReadOnlyDays)
            ? CompanyLicenseAccessMode.OwnerReadOnly
            : CompanyLicenseAccessMode.Operational;
    }
}

public sealed record LicenseLifecyclePolicy(
    int TrialToleranceDays,
    int TrialTerminationDays,
    int PaidReadOnlyDays,
    int PaidTerminationDays)
{
    public static LicenseLifecyclePolicy Default { get; } = new(3, 10, 7, 20);
}
