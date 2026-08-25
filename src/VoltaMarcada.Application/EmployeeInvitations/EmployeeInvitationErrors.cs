using VoltaMarcada.Application.Abstractions.Operations;

namespace VoltaMarcada.Application.EmployeeInvitations;

public static class EmployeeInvitationErrors
{
    public static readonly OperationError InvalidInput =
        new("EmployeeInvitation.InvalidInput", OperationErrorType.Validation, "Employee invitation input is invalid.");

    public static readonly OperationError Unauthenticated =
        new("EmployeeInvitation.Unauthenticated", OperationErrorType.Forbidden, "An authenticated user is required.");

    public static readonly OperationError Unauthorized =
        new("EmployeeInvitation.Unauthorized", OperationErrorType.Forbidden, "The current user cannot manage employee invitations.");

    public static readonly OperationError EmailAlreadyRegistered =
        new("EmployeeInvitation.EmailAlreadyRegistered", OperationErrorType.Conflict, "The email address is already registered.");

    public static readonly OperationError DuplicateEmail =
        new("EmployeeInvitation.DuplicateEmail", OperationErrorType.Conflict, "An invitation already exists for this email address.");

    public static readonly OperationError DuplicateDocument =
        new("EmployeeInvitation.DuplicateDocument", OperationErrorType.Conflict, "A company member or invitation already uses this document.");

    public static readonly OperationError InvitationNotFound =
        new("EmployeeInvitation.NotFound", OperationErrorType.NotFound, "Employee invitation was not found.");

    public static readonly OperationError InvitationUnavailable =
        new("EmployeeInvitation.Unavailable", OperationErrorType.Conflict, "Employee invitation is no longer available.");
}
