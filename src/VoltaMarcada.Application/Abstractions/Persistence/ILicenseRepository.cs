using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public sealed record CompanyLicenseMembership(
    CompanyUserRole Role,
    CompanyLicense License);

public enum LicensePaymentPersistenceOutcome
{
    Success,
    PendingPaymentAlreadyExists
}

public interface ILicenseRepository
{
    Task<CompanyLicenseMembership?> GetMembershipAndLicenseAsync(
        Guid userId,
        CancellationToken cancellationToken = default);

    Task<CompanyLicense?> GetForCompanyAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<bool> HasPendingPaymentAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<LicensePayment?> GetPendingPaymentAsync(
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(
        LicensePayment payment,
        CancellationToken cancellationToken = default);
}
