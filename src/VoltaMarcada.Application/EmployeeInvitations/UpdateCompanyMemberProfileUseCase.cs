using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.EmployeeInvitations;

public sealed class UpdateCompanyMemberProfileUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
    IEmployeeInvitationAccountService accountService,
    IValidator<UpdateCompanyMemberProfileCommand> validator)
{
    public async Task<OperationResult<EmployeeInvitationOutcome>> ExecuteAsync(
        UpdateCompanyMemberProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthenticated);
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var document))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvalidInput);
        }

        if (await currentCompany.FindCompanyIdAsync(cancellationToken) is not { } companyId)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthorized);
        }

        if (!await repository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthorized);
        }

        var userId = await repository.GetUserIdByMembershipIdAsync(command.MembershipId, companyId, cancellationToken);
        if (userId is null)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvitationNotFound);
        }

        var outcome = await accountService.UpdateProfileAsync(
            userId.Value,
            command.FullName,
            document,
            command.Phone,
            command.Address,
            cancellationToken);
        return outcome switch
        {
            EmployeeProfileUpdateOutcome.Success => OperationResult<EmployeeInvitationOutcome>.Success(new EmployeeInvitationOutcome()),
            EmployeeProfileUpdateOutcome.UserNotFound => OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvitationNotFound),
            EmployeeProfileUpdateOutcome.DuplicateDocument => OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.DuplicateDocument),
            _ => throw new InvalidOperationException($"Unsupported employee profile update outcome: {outcome}.")
        };
    }
}
