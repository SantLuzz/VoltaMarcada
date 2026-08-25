using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Professionals;

public sealed class ChangeProfessionalStatusUseCase(
    ICurrentCompany currentCompany,
    IProfessionalRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(Guid professionalId, bool isActive, CancellationToken cancellationToken = default)
    {
        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (!await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.Forbidden);
        }

        var professional = await repository.GetForCompanyAsync(professionalId, companyId, cancellationToken);
        if (professional is null)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.NotFound);
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
        return OperationResult<Guid?>.Success(professional.Id);
    }
}
