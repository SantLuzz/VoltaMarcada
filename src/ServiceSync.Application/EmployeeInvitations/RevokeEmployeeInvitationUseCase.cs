using ServiceSync.Application.Abstractions.Operations;
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
    public async Task<OperationResult<EmployeeInvitationOutcome>> ExecuteAsync(
        Guid invitationId,
        CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthenticated);
        }

        if (await currentCompany.FindCompanyIdAsync(cancellationToken) is not { } companyId)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthorized);
        }

        if (!await repository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.Unauthorized);
        }

        var invitation = await repository.GetByIdAsync(invitationId, companyId, cancellationToken);
        if (invitation is null)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvitationNotFound);
        }

        var now = timeProvider.GetUtcNow();
        if (!invitation.IsPending(now))
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvitationUnavailable);
        }

        invitation.Revoke(now);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<EmployeeInvitationOutcome>.Success(new EmployeeInvitationOutcome());
    }
}
