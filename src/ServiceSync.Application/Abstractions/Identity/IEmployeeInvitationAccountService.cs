namespace ServiceSync.Application.Abstractions.Identity;

public interface IEmployeeInvitationAccountService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<EmployeeInvitationAccountResult> CreateAccountAndAcceptAsync(
        string tokenHash,
        string password,
        DateTimeOffset now,
        CancellationToken cancellationToken = default);

    Task<EmployeeProfileUpdateOutcome> UpdateProfileAsync(
        Guid userId,
        string fullName,
        string document,
        string phone,
        string address,
        CancellationToken cancellationToken = default);
}

public sealed record EmployeeInvitationAccountResult(
    EmployeeInvitationAccountOutcome Outcome,
    Guid? UserId)
{
    public static EmployeeInvitationAccountResult Success(Guid userId) =>
        new(EmployeeInvitationAccountOutcome.Success, userId);

    public static EmployeeInvitationAccountResult InvitationUnavailable() =>
        new(EmployeeInvitationAccountOutcome.InvitationUnavailable, null);

    public static EmployeeInvitationAccountResult EmailAlreadyRegistered() =>
        new(EmployeeInvitationAccountOutcome.EmailAlreadyRegistered, null);
}

public enum EmployeeInvitationAccountOutcome
{
    Success,
    InvitationUnavailable,
    EmailAlreadyRegistered,
    InvalidPassword
}

public enum EmployeeProfileUpdateOutcome
{
    Success,
    UserNotFound,
    DuplicateDocument
}
