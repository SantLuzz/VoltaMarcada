namespace ServiceSync.Application.EmployeeInvitations;

public sealed record EmployeeInvitationResult(
    EmployeeInvitationResultCode Code,
    string? Token = null,
    Guid? UserId = null)
{
    public static EmployeeInvitationResult Success(string? token = null, Guid? userId = null) =>
        new(EmployeeInvitationResultCode.Success, token, userId);
}

public enum EmployeeInvitationResultCode
{
    Success,
    InvalidInput,
    Unauthenticated,
    Unauthorized,
    EmailAlreadyRegistered,
    DuplicateEmail,
    DuplicateDocument,
    InvitationNotFound,
    InvitationUnavailable
}
