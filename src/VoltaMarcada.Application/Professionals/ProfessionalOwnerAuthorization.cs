using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;

namespace VoltaMarcada.Application.Professionals;

public sealed class ProfessionalOwnerAuthorization(
    ICurrentUser currentUser,
    IEmployeeInvitationRepository employeeInvitationRepository)
{
    public Task<bool> IsOwnerAsync(Guid companyId, CancellationToken cancellationToken = default) =>
        currentUser.IsAuthenticated && currentUser.UserId != Guid.Empty
            ? employeeInvitationRepository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken)
            : Task.FromResult(false);
}
