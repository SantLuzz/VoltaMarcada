using FluentValidation;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class UpdateCompanyMemberProfileUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
    IEmployeeInvitationAccountService accountService,
    IValidator<UpdateCompanyMemberProfileCommand> validator)
{
    public async Task<EmployeeInvitationResult> ExecuteAsync(
        UpdateCompanyMemberProfileCommand command,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthenticated);
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var document))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvalidInput);
        }

        Guid companyId;
        try
        {
            companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        }
        catch (CurrentCompanyNotFoundException)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthorized);
        }

        if (!await repository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthorized);
        }

        var userId = await repository.GetUserIdByMembershipIdAsync(command.MembershipId, companyId, cancellationToken);
        if (userId is null)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvitationNotFound);
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
            EmployeeProfileUpdateOutcome.Success => EmployeeInvitationResult.Success(),
            EmployeeProfileUpdateOutcome.UserNotFound => new EmployeeInvitationResult(EmployeeInvitationResultCode.InvitationNotFound),
            EmployeeProfileUpdateOutcome.DuplicateDocument => new EmployeeInvitationResult(EmployeeInvitationResultCode.DuplicateDocument),
            _ => throw new InvalidOperationException($"Unsupported employee profile update outcome: {outcome}.")
        };
    }
}
