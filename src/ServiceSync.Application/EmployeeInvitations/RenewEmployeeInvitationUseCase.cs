using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class RenewEmployeeInvitationUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
    CreateEmployeeInvitationUseCase createEmployeeInvitationUseCase,
    TimeProvider timeProvider)
{
    public async Task<EmployeeInvitationResult> ExecuteAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.Unauthenticated);
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

        var invitation = await repository.GetByIdAsync(invitationId, companyId, cancellationToken);
        if (invitation is null || invitation.AcceptedAtUtc is not null)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvitationNotFound);
        }

        if (invitation.IsPending(timeProvider.GetUtcNow()))
        {
            invitation.Revoke(timeProvider.GetUtcNow());
            await repository.SaveChangesAsync(cancellationToken);
        }

        return await createEmployeeInvitationUseCase.ExecuteAsync(
            new CreateEmployeeInvitationCommand(
                invitation.FullName,
                invitation.Email,
                invitation.Document,
                invitation.Phone,
                invitation.Address),
            cancellationToken);
    }
}
