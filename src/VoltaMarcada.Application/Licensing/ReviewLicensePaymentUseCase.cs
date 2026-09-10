using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.Licensing;

public sealed class ReviewLicensePaymentUseCase(
    ILicenseRepository repository,
    ICurrentUser currentUser,
    TimeProvider timeProvider)
{
    public async Task<OperationResult> ExecuteAsync(
        Guid paymentId,
        LicensePaymentReviewCommand command,
        CancellationToken cancellationToken = default)
    {
        if (paymentId == Guid.Empty || !Enum.IsDefined(command.Decision))
        {
            return OperationResult.Failure(LicenseErrors.InvalidPaymentReview);
        }

        if (command.Decision == LicensePaymentReviewDecision.Reject && string.IsNullOrWhiteSpace(command.Reason))
        {
            return OperationResult.Failure(LicenseErrors.RejectionReasonRequired);
        }

        var outcome = await repository.ReviewPaymentAsync(
            paymentId,
            currentUser.UserId,
            command,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return outcome == LicensePaymentReviewPersistenceOutcome.Success
            ? OperationResult.Success()
            : OperationResult.Failure(LicenseErrors.PaymentNotPendingReview);
    }
}
