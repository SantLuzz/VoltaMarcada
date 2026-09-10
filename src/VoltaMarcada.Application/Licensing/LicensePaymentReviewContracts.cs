namespace VoltaMarcada.Application.Licensing;

public enum LicensePaymentReviewDecision
{
    Approve,
    Reject
}

public sealed record LicensePaymentReviewCommand(
    LicensePaymentReviewDecision Decision,
    string? Reason);
