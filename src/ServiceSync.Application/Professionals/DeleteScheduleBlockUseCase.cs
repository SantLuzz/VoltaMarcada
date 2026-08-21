using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;

namespace ServiceSync.Application.Professionals;

public sealed class DeleteScheduleBlockUseCase(
    ICurrentCompany currentCompany,
    IProfessionalAvailabilityRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization)
{
    public async Task<ProfessionalAvailabilityOperationResult> ExecuteAsync(
        Guid professionalId,
        Guid scheduleBlockId,
        CancellationToken cancellationToken = default)
    {
        if (professionalId == Guid.Empty || scheduleBlockId == Guid.Empty)
        {
            return ProfessionalAvailabilityOperationResult.InvalidInput();
        }

        var companyId = await currentCompany.GetCompanyIdAsync(cancellationToken);
        if (!await ownerAuthorization.IsOwnerAsync(companyId, cancellationToken))
        {
            return ProfessionalAvailabilityOperationResult.Forbidden();
        }

        if (!await repository.ProfessionalExistsAsync(professionalId, companyId, cancellationToken))
        {
            return ProfessionalAvailabilityOperationResult.ProfessionalNotFound();
        }

        var scheduleBlock = await repository.GetScheduleBlockAsync(
            scheduleBlockId,
            professionalId,
            companyId,
            cancellationToken);
        if (scheduleBlock is null)
        {
            return ProfessionalAvailabilityOperationResult.ScheduleBlockNotFound();
        }

        repository.RemoveScheduleBlock(scheduleBlock);
        await repository.SaveChangesAsync(cancellationToken);
        return ProfessionalAvailabilityOperationResult.Success();
    }
}
