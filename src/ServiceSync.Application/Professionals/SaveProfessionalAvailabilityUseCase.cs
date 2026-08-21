using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.Professionals;

public sealed class SaveProfessionalAvailabilityUseCase(
    ICurrentCompany currentCompany,
    IProfessionalAvailabilityRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    IValidator<SaveProfessionalAvailabilityCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<ProfessionalAvailabilityOperationResult> ExecuteAsync(
        Guid professionalId,
        SaveProfessionalAvailabilityCommand command,
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

        var now = timeProvider.GetUtcNow();
        var dailyScheduleDefinitions = command.DailySchedules
            .Select(schedule => new ProfessionalDailyScheduleDefinition(
                schedule.Weekday,
                schedule.EntryTime,
                schedule.ExitTime,
                schedule.LunchStartTime,
                schedule.LunchEndTime))
            .ToArray();
        var availability = await repository.GetAvailabilityAsync(
            professionalId,
            companyId,
            trackChanges: true,
            cancellationToken);
        if (availability is null)
        {
            availability = ProfessionalAvailability.Create(
                companyId,
                professionalId,
                dailyScheduleDefinitions,
                now);
            await repository.AddAvailabilityAsync(availability, cancellationToken);
        }
        else
        {
            availability.Update(
                dailyScheduleDefinitions,
                now);
        }

        await repository.SaveChangesAsync(cancellationToken);
        return ProfessionalAvailabilityOperationResult.Success();
    }
}
