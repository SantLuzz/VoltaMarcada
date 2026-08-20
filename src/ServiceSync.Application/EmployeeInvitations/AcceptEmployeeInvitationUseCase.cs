using ServiceSync.Application.Abstractions.Identity;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class AcceptEmployeeInvitationUseCase(
    IEmployeeInvitationAccountService accountService,
    TimeProvider timeProvider)
{
    public async Task<EmployeeInvitationResult> ExecuteAsync(
        string? token,
        string? password,
        string? confirmPassword,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token) ||
            string.IsNullOrWhiteSpace(password) ||
            password.Length < 6 ||
            password != confirmPassword)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvalidInput);
        }

        var result = await accountService.CreateAccountAndAcceptAsync(
            EmployeeInvitationToken.CreateHash(token),
            password,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return result.Outcome switch
        {
            EmployeeInvitationAccountOutcome.Success => EmployeeInvitationResult.Success(userId: result.UserId),
            EmployeeInvitationAccountOutcome.EmailAlreadyRegistered => new EmployeeInvitationResult(EmployeeInvitationResultCode.EmailAlreadyRegistered),
            EmployeeInvitationAccountOutcome.InvalidPassword => new EmployeeInvitationResult(EmployeeInvitationResultCode.InvalidInput),
            EmployeeInvitationAccountOutcome.InvitationUnavailable => new EmployeeInvitationResult(EmployeeInvitationResultCode.InvitationUnavailable),
            _ => throw new InvalidOperationException($"Unsupported employee invitation account outcome: {result.Outcome}.")
        };
    }
}
