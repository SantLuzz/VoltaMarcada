using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Professionals;

public sealed class CreateScheduleBlockUseCase(
    ICurrentCompany currentCompany,
    IProfessionalAvailabilityRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    IValidator<CreateScheduleBlockCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<ProfessionalAvailabilityOperationResult> ExecuteAsync(
        Guid professionalId,
        CreateScheduleBlockCommand command,
        CancellationToken cancellationToken = default)
    {
        if (professionalId == Guid.Empty || !(await validator.ValidateAsync(command, cancellationToken)).IsValid)
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

        var scheduleBlock = ScheduleBlock.Create(
            companyId,
            professionalId,
            command.Type,
            command.StartDate,
            command.EndDate,
            command.IsFullDay,
            command.StartTime,
            command.EndTime,
            command.Notes,
            timeProvider.GetUtcNow());
        await repository.AddScheduleBlockAsync(scheduleBlock, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return ProfessionalAvailabilityOperationResult.Success(scheduleBlock.Id);
    }
}
