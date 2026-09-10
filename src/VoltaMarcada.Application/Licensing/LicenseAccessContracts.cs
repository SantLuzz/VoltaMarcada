using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Licensing;

public enum CompanyLicenseAction
{
    None,
    Join,
    Renew
}

public sealed record CompanyLicenseAccessDto(
    CompanyLicenseStatus Status,
    CompanyLicenseAccessMode AccessMode,
    DateTimeOffset ExpiresAtUtc,
    int DaysRemaining,
    bool IsOverdue,
    CompanyLicenseAction Action);
