using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderAccessScope(Guid CompanyId, bool IsOwner, Guid? ProfessionalId);

public sealed class WorkOrderAuthorization(ICurrentCompany currentCompany, ICurrentUser currentUser, IEmployeeInvitationRepository employeeInvitationRepository, IProfessionalRepository professionalRepository)
{
    public async Task<WorkOrderAccessScope> GetAccessScopeAsync(CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (await employeeInvitationRepository.IsCurrentUserOwnerAsync(currentUser.UserId, companyId, cancellationToken)) return new WorkOrderAccessScope(companyId, true, null);
        var professional = await professionalRepository.GetForCurrentUserAsync(currentUser.UserId, companyId, cancellationToken);
        var professionalId = professional is not null && professional.Professional.IsActive
            ? professional.Professional.Id
            : null;

        return new WorkOrderAccessScope(companyId, false, professionalId);
    }
}
