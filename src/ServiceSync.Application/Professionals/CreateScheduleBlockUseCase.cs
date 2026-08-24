using ServiceSync.Application.Abstractions.Operations;
using FluentValidation;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Tenancy;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Professionals;

public sealed class CreateScheduleBlockUseCase(
    ICurrentCompany currentCompany,
    IProfessionalAvailabilityRepository repository,
    IAppointmentRepository appointmentRepository,
    IProfessionalRepository professionalRepository,
    ProfessionalOwnerAuthorization ownerAuthorization,
    IValidator<CreateScheduleBlockCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid?>> ExecuteAsync(
        Guid professionalId,
        CreateScheduleBlockCommand command,
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

        var professional = await professionalRepository.GetForCompanyAsync(professionalId, companyId, cancellationToken);
        if (professional is null)
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.AvailabilityProfessionalNotFound);
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
        if (!TryGetUtcInterval(scheduleBlock, professional.TimeZoneId, out var startAtUtc, out var endAtUtc))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.AvailabilityInvalidInput);
        }

        await using var transaction = await appointmentRepository.BeginSchedulingTransactionAsync(companyId, professionalId, cancellationToken);
        if (await appointmentRepository.HasAppointmentConflictAsync(companyId, professionalId, startAtUtc, endAtUtc, null, cancellationToken))
        {
            return OperationResult<Guid?>.Failure(ProfessionalErrors.ScheduleBlockConflictsWithAppointment);
        }

        await repository.AddScheduleBlockAsync(scheduleBlock, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid?>.Success(scheduleBlock.Id);
    }

    private static bool TryGetUtcInterval(ScheduleBlock scheduleBlock, string timeZoneId, out DateTimeOffset startAtUtc, out DateTimeOffset endAtUtc)
    {
        var startTime = scheduleBlock.IsFullDay ? TimeOnly.MinValue : scheduleBlock.StartTime!.Value;
        var endDate = scheduleBlock.IsFullDay ? scheduleBlock.EndDate.AddDays(1) : scheduleBlock.EndDate;
        var endTime = scheduleBlock.IsFullDay ? TimeOnly.MinValue : scheduleBlock.EndTime!.Value;
        if (!global::ServiceSync.Application.Scheduling.AppointmentAvailability.TryGetUtcInterval(timeZoneId, scheduleBlock.StartDate, startTime, 1, out startAtUtc, out _))
        {
            endAtUtc = default;
            return false;
        }

        return global::ServiceSync.Application.Scheduling.AppointmentAvailability.TryGetUtcInterval(timeZoneId, endDate, endTime, 1, out endAtUtc, out _);
    }
}
