using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class RevokeEmployeeInvitationUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
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
        if (invitation is null)
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvitationNotFound);
        }

        var now = timeProvider.GetUtcNow();
        if (!invitation.IsPending(now))
        {
            return new EmployeeInvitationResult(EmployeeInvitationResultCode.InvitationUnavailable);
        }

        invitation.Revoke(now);
        await repository.SaveChangesAsync(cancellationToken);
        return EmployeeInvitationResult.Success();
    }
}
