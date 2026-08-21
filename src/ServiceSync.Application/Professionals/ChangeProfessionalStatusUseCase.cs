using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Professionals;

public sealed class ChangeProfessionalStatusUseCase(
    ICurrentCompany currentCompany,
    IProfessionalRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    TimeProvider timeProvider)
{
    public async Task<ProfessionalOperationResult> ExecuteAsync(Guid professionalId, bool isActive, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (!await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken))
        {
            return ProfessionalOperationResult.Forbidden();
        }

        var professional = await repository.GetForCompanyAsync(professionalId, companyId, cancellationToken);
        if (professional is null)
        {
            return ProfessionalOperationResult.NotFound();
        }

        if (isActive)
        {
            professional.Activate(timeProvider.GetUtcNow());
        }
        else
        {
            professional.Deactivate(timeProvider.GetUtcNow());
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ProfessionalOperationResult.Success(professional.Id);
    }
}
