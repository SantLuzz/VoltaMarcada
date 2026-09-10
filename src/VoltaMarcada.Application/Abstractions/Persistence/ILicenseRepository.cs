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

public enum LicensePaymentReviewPersistenceOutcome
{
    Success,
    NotPending
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

    Task<LicensePayment?> GetPaymentAsync(
        Guid paymentId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<LicensePaymentPersistenceOutcome> AddPaymentAsync(
        LicensePayment payment,
        CancellationToken cancellationToken = default);

    Task<LicensePaymentReviewPersistenceOutcome> ReviewPaymentAsync(
        Guid paymentId,
        Guid administratorId,
        Licensing.LicensePaymentReviewCommand command,
        DateTimeOffset now,
        CancellationToken cancellationToken = default) =>
        throw new NotSupportedException();
    Task<IReadOnlyList<Licensing.PlatformLicensePaymentDto>> GetPlatformPaymentsAsync(
        CancellationToken cancellationToken = default) =>
        Task.FromResult<IReadOnlyList<Licensing.PlatformLicensePaymentDto>>([]);
    Task<LicensePayment?> GetPaymentForPlatformAsync(
        Guid paymentId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult<LicensePayment?>(null);
}
