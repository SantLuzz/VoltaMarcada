using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Licensing;

public static class LicenseErrors
{
    public static readonly OperationError InvalidPaymentProof =
        new(
            "License.InvalidPaymentProof",
            OperationErrorType.Validation,
            "Payment proof is invalid.");

    public static readonly OperationError PendingPaymentAlreadyExists =
        new(
            "License.PendingPaymentAlreadyExists",
            OperationErrorType.Conflict,
            "A license payment is already pending review.");
}
