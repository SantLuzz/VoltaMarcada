using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Web.Components.Pages;
using Xunit;

namespace VoltaMarcada.IntegrationTests.Components;

public sealed class PlatformLicensePaymentFilterTests
{
    [Fact]
    public void Apply_returns_only_pending_payments_submitted_in_the_selected_month()
    {
        var septemberPending = Payment(
            submittedAtUtc: new DateTimeOffset(2026, 9, 4, 12, 0, 0, TimeSpan.Zero),
            status: LicensePaymentStatus.PendingReview);
        var septemberApproved = Payment(
            submittedAtUtc: new DateTimeOffset(2026, 9, 5, 12, 0, 0, TimeSpan.Zero),
            status: LicensePaymentStatus.Approved);
        var augustPending = Payment(
            submittedAtUtc: new DateTimeOffset(2026, 8, 31, 12, 0, 0, TimeSpan.Zero),
            status: LicensePaymentStatus.PendingReview);

        var result = PlatformLicensePaymentFilter.Apply(
            [septemberPending, septemberApproved, augustPending],
            new DateTime(2026, 9, 1),
            pendingOnly: true);

        var payment = Assert.Single(result);
        Assert.Equal(septemberPending.Id, payment.Id);
    }

    private static PlatformLicensePaymentDto Payment(
        DateTimeOffset submittedAtUtc,
        LicensePaymentStatus status) =>
        new(
            Guid.NewGuid(),
            "Example company",
            "owner@example.com",
            null,
            49.90m,
            submittedAtUtc,
            status,
            null,
            null);
}
