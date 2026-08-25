using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;

namespace VoltaMarcada.Application.Professionals;

public sealed class DeleteScheduleBlockUseCase(
    ICurrentCompany currentCompany,
    IProfessionalAvailabilityRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        Guid professionalId,
        Guid scheduleBlockId,
        CancellationToken cancellationToken = default)
    {
        if (professionalId == Guid.Empty || scheduleBlockId == Guid.Empty)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.AvailabilityInvalidInput);
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (!await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.AvailabilityForbidden);
        }

        if (!await repository.ProfessionalExistsAsync(professionalId, companyId, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.AvailabilityProfessionalNotFound);
        }

        var scheduleBlock = await repository.GetScheduleBlockAsync(
            scheduleBlockId,
            professionalId,
            companyId,
            cancellationToken);
        if (scheduleBlock is null)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.ScheduleBlockNotFound);
        }

        repository.RemoveScheduleBlock(scheduleBlock);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid?>.Success(null);
    }
}
