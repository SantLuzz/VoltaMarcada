using ServiceSync.Application.Abstractions.Operations;
using ServiceSync.Application.Abstractions.Identity;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class AcceptEmployeeInvitationUseCase(
    IEmployeeInvitationAccountService accountService,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<EmployeeInvitationOutcome>> ExecuteAsync(
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
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvalidInput);
        }

        var result = await accountService.CreateAccountAndAcceptAsync(
            EmployeeInvitationToken.CreateHash(token),
            password,
            timeProvider.GetUtcNow(),
            cancellationToken);

        return result.Outcome switch
        {
            EmployeeInvitationAccountOutcome.Success => OperationResult<EmployeeInvitationOutcome>.Success(new EmployeeInvitationOutcome(UserId: result.UserId)),
            EmployeeInvitationAccountOutcome.EmailAlreadyRegistered => OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.EmailAlreadyRegistered),
            EmployeeInvitationAccountOutcome.InvalidPassword => OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvalidInput),
            EmployeeInvitationAccountOutcome.InvitationUnavailable => OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvitationUnavailable),
            _ => throw new InvalidOperationException($"Unsupported employee invitation account outcome: {result.Outcome}.")
        };
    }
}
