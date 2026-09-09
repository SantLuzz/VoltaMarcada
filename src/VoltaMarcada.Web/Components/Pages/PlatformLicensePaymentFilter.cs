using VoltaMarcada.Application.Licensing;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Web.Components.Pages;

public static class PlatformLicensePaymentFilter
{
    public static IReadOnlyList<PlatformLicensePaymentDto> Apply(
        IEnumerable<PlatformLicensePaymentDto> payments,
        DateTime? selectedMonth,
        bool pendingOnly)
    {
        ArgumentNullException.ThrowIfNull(payments);

        var filteredPayments = payments;
        if (selectedMonth is { } month)
        {
            filteredPayments = filteredPayments.Where(payment =>
                payment.Id != Guid.Empty &&
                payment.SubmittedAtUtc.Year == month.Year &&
                payment.SubmittedAtUtc.Month == month.Month);
        }

        if (pendingOnly)
        {
            filteredPayments = filteredPayments.Where(payment =>
                payment.Status == LicensePaymentStatus.PendingReview);
        }

        return filteredPayments.ToList();
    }
}
