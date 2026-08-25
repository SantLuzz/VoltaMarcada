using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.EmployeeInvitations;

public sealed class RenewEmployeeInvitationUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
    CreateEmployeeInvitationUseCase createEmployeeInvitationUseCase,
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
        if (invitation is null || invitation.AcceptedAtUtc is not null)
        {
            return OperationResult<EmployeeInvitationOutcome>.Failure(EmployeeInvitationErrors.InvitationNotFound);
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
