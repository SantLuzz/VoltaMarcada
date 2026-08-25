using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Tenancy;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.Professionals;

public sealed class SaveProfessionalAvailabilityUseCase(
    ICurrentCompany currentCompany,
    IProfessionalAvailabilityRepository repository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    IValidator<SaveProfessionalAvailabilityCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        Guid professionalId,
        SaveProfessionalAvailabilityCommand command,
        CancellationToken cancellationToken = default)
    {
        if (professionalId == Guid.Empty || !(await validator.ValidateAsync(command, cancellationToken)).IsValid)
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
        return OperationResult<Guid?>.Success(null);
    }
}
