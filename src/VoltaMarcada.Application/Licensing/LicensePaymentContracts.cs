using VoltaMarcada.Application.Abstractions.Payments;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Licensing;

public interface ILicenseBillingConfiguration
{
    decimal MonthlyPrice { get; }

    string PixKey { get; }

    string BeneficiaryName { get; }

    string BeneficiaryCity { get; }

    long MaximumProofSizeBytes { get; }
}

public sealed record LicensePaymentProofUpload(FileUpload File);

public sealed record LicensePaymentSubmissionDto(
    Guid Id,
    decimal Amount,
    LicensePaymentStatus Status,
    DateTimeOffset SubmittedAtUtc);

public sealed record LicenseRenewalDto(
    decimal MonthlyPrice,
    PixPaymentPayload Pix,
    LicensePaymentSubmissionDto? PendingPayment);
