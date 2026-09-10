using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.Exceptions;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Licensing;

public sealed class CompanyLicenseTests
{
    [Fact]
    public void GrantManualAccess_sets_the_exact_expiration_selected_by_the_administrator()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var license = CompanyLicense.Create(Guid.NewGuid(), now, 14);
        var expiration = new DateTimeOffset(2026, 12, 25, 18, 0, 0, TimeSpan.FromHours(-3));

        license.GrantManualAccess(expiration, now);

        Assert.Equal(CompanyLicenseStatus.Paid, license.Status);
        Assert.Equal(expiration.ToUniversalTime(), license.PaidUntilUtc);
    }

    [Fact]
    public void GrantManualAccess_rejects_an_expiration_that_has_already_passed()
    {
        var now = new DateTimeOffset(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);
        var license = CompanyLicense.Create(Guid.NewGuid(), now, 14);

        Assert.Throws<DomainException>(() => license.GrantManualAccess(now, now));
    }
    [Fact]
    public void Returns_operational_during_trial_tolerance()
    {
        var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);

        var accessMode = license.GetAccessMode(
            new DateTimeOffset(2026, 9, 17, 0, 0, 0, TimeSpan.Zero),
            LicenseLifecyclePolicy.Default);

        Assert.Equal(CompanyLicenseAccessMode.Operational, accessMode);
    }

    [Fact]
    public void Returns_read_only_after_trial_tolerance()
    {
        var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);

        Assert.Equal(
            CompanyLicenseAccessMode.OwnerReadOnly,
            license.GetAccessMode(new DateTimeOffset(2026, 9, 19, 0, 0, 0, TimeSpan.Zero), LicenseLifecyclePolicy.Default));
    }

    [Fact]
    public void Returns_terminated_on_tenth_day_after_trial_end()
    {
        var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);

        var accessMode = license.GetAccessMode(
            new DateTimeOffset(2026, 9, 25, 0, 0, 0, TimeSpan.Zero),
            LicenseLifecyclePolicy.Default);

        Assert.Equal(CompanyLicenseAccessMode.Terminated, accessMode);
    }

    [Fact]
    public void Renew_preserves_unexpired_paid_days()
    {
        var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);

        license.Renew(new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero));
        license.Renew(new DateTimeOffset(2026, 9, 15, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(new DateTimeOffset(2026, 11, 2, 0, 0, 0, TimeSpan.Zero), license.PaidUntilUtc);
    }

    [Fact]
    public void Returns_read_only_after_paid_overdue_threshold()
    {
        var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);
        license.Renew(new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero));

        var accessMode = license.GetAccessMode(
            new DateTimeOffset(2026, 10, 9, 0, 0, 0, TimeSpan.Zero),
            LicenseLifecyclePolicy.Default);

        Assert.Equal(CompanyLicenseAccessMode.OwnerReadOnly, accessMode);
    }

    [Fact]
    public void Returns_terminated_on_twentieth_paid_overdue_day()
    {
        var license = CompanyLicense.Create(Guid.NewGuid(), new DateTimeOffset(2026, 9, 1, 0, 0, 0, TimeSpan.Zero), 14);
        license.Renew(new DateTimeOffset(2026, 9, 2, 0, 0, 0, TimeSpan.Zero));

        var accessMode = license.GetAccessMode(
            new DateTimeOffset(2026, 10, 22, 0, 0, 0, TimeSpan.Zero),
            LicenseLifecyclePolicy.Default);

        Assert.Equal(CompanyLicenseAccessMode.Terminated, accessMode);
    }
}
