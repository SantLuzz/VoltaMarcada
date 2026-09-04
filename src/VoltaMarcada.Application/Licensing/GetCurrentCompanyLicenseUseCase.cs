using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Licensing;

public sealed class GetCurrentCompanyLicenseUseCase(
    ILicenseRepository licenseRepository,
    ICurrentCompany currentCompany,
    TimeProvider timeProvider)
{
    public async Task<CompanyLicenseAccessDto?> ExecuteAsync(
        CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        var license = await licenseRepository.GetForCompanyAsync(companyId, cancellationToken);
        if (license is null)
        {
            return null;
        }

        var now = timeProvider.GetUtcNow();
        var expiresAtUtc = license.PaidUntilUtc ?? license.TrialEndsAtUtc;
        var daysRemaining = (int)Math.Ceiling((expiresAtUtc - now).TotalDays);
        return new CompanyLicenseAccessDto(
            license.Status,
            license.GetAccessMode(now, LicenseLifecyclePolicy.Default),
            expiresAtUtc,
            daysRemaining,
            now > expiresAtUtc,
            GetAction(license.Status, daysRemaining));
    }

    private static CompanyLicenseAction GetAction(
        CompanyLicenseStatus status,
        int daysRemaining) => status switch
        {
            CompanyLicenseStatus.Trial when daysRemaining is >= 0 and <= 3 => CompanyLicenseAction.Join,
            CompanyLicenseStatus.Trial when daysRemaining < 0 => CompanyLicenseAction.Renew,
            CompanyLicenseStatus.Paid when daysRemaining <= 7 => CompanyLicenseAction.Renew,
            _ => CompanyLicenseAction.None
        };
}
