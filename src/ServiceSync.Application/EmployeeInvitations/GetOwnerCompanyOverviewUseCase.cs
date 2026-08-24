using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.EmployeeInvitations;

public sealed class GetOwnerCompanyOverviewUseCase(
    ICurrentUser currentUser,
    ICurrentCompany currentCompany,
    IEmployeeInvitationRepository repository,
    TimeProvider timeProvider)
{
    public async Task<CompanyOverviewDto?> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return null;
        }

        if (await currentCompany.FindCompanyIdAsync(cancellationToken) is not { } companyId)
        {
            return null;
        }

        if (!await repository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken))
        {
            return null;
        }

        return await repository.GetCompanyOverviewAsync(companyId, timeProvider.GetUtcNow(), cancellationToken);
    }
}
