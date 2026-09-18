using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.Support;

public static class SupportErrors
{
    public static readonly OperationError Unauthenticated = new("Support.Unauthenticated", OperationErrorType.Forbidden, "An authenticated user is required.");
    public static readonly OperationError CompanyUnavailable = new("Support.CompanyUnavailable", OperationErrorType.Forbidden, "The current company membership is unavailable.");
    public static readonly OperationError InvalidInput = new("Support.InvalidInput", OperationErrorType.Validation, "The support request text is invalid.");
    public static readonly OperationError InvalidAttachment = new("Support.InvalidAttachment", OperationErrorType.Validation, "An attachment is invalid.");
    public static readonly OperationError UserEmailUnavailable = new("Support.UserEmailUnavailable", OperationErrorType.Validation, "The authenticated user email is unavailable.");
    public static readonly OperationError RecipientUnavailable = new("Support.RecipientUnavailable", OperationErrorType.Validation, "The support recipient is unavailable.");
}
