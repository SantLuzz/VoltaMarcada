using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Licensing;

public static class LicenseErrors
{
    public static readonly OperationError InvalidPaymentReview =
        new("License.InvalidPaymentReview", OperationErrorType.Validation, "Payment review is invalid.");

    public static readonly OperationError RejectionReasonRequired =
        new("License.RejectionReasonRequired", OperationErrorType.Validation, "Payment rejection reason is required.");

    public static readonly OperationError PaymentNotPendingReview =
        new("License.PaymentNotPendingReview", OperationErrorType.Conflict, "Payment is not pending review.");

    public static readonly OperationError InvalidPaymentProof =
        new("License.InvalidPaymentProof", OperationErrorType.Validation, "Payment proof is invalid.");

    public static readonly OperationError RenewalNotEligible =
        new("License.RenewalNotEligible", OperationErrorType.Validation, "License is not eligible for renewal.");

    public static readonly OperationError PendingPaymentAlreadyExists =
        new("License.PendingPaymentAlreadyExists", OperationErrorType.Conflict, "A license payment is already pending review.");

    public static readonly OperationError InvalidManualLicenseExpiration =
        new("License.InvalidManualLicenseExpiration", OperationErrorType.Validation, "License expiration must be in the future.");

    public static readonly OperationError CompanyLicenseNotFound =
        new("License.CompanyLicenseNotFound", OperationErrorType.NotFound, "Company license was not found.");
}