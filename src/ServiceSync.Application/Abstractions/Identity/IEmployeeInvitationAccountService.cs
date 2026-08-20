namespace ServiceSync.Application.Abstractions.Identity;

public interface IEmployeeInvitationAccountService
{
    Task<bool> EmailExistsAsync(string email, CancellationToken cancellationToken = default);

    Task<EmployeeInvitationAccountResult> CreateAccountAndAcceptAsync(
        string tokenHash,
        string password,
        DateTimeOffset now,
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
