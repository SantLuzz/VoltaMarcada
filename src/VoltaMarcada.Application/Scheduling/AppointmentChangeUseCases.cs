using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Scheduling;

public sealed class UpdateAppointmentNotesUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
        Guid appointmentId,
        UpdateAppointmentNotesCommand command,
        CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        }

        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        }

        if (appointment.Status != global::VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        }

        if (command.Notes?.Trim().Length > 1000)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);
        }

        appointment.UpdateNotes(command.Notes, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class RescheduleAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository, AppointmentAvailability availability, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid appointmentId, RescheduleAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (appointment.Status != global::VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        if (string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 1000) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);
        var professional = await professionalRepository.GetForCompanyAsync(appointment.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        var duration = (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes;
        await using var transaction = await repository.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);
        if (!await availability.IsAvailableAsync(professional, command.Date, command.StartTime, duration, appointment.Id, cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, duration, out var startAtUtc, out var endAtUtc)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);
        appointment.Reschedule(startAtUtc, endAtUtc, command.Reason, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, timeProvider.GetUtcNow());
        if (await repository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict) return OperationResult<Guid>.Failure(SchedulingErrors.AppointmentConflict);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class ReassignAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository, AppointmentAvailability availability, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid appointmentId, Guid professionalId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (appointment is null || professional is null || !professional.IsActive) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (appointment.Status != global::VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        if (appointment.ServiceId is { } serviceId && professional.ProfessionalServices.All(item => item.ServiceId != serviceId)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalNotQualified);
        var localStart = TimeZoneInfo.ConvertTime(appointment.StartAtUtc, TimeZoneInfo.FindSystemTimeZoneById(professional.TimeZoneId));
        var duration = (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes;
        await using var transaction = await repository.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);
        if (!await availability.IsAvailableAsync(professional, DateOnly.FromDateTime(localStart.Date), TimeOnly.FromDateTime(localStart.DateTime), duration, appointment.Id, cancellationToken)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);
        appointment.Reassign(professionalId, timeProvider.GetUtcNow());
        if (await repository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict) return OperationResult<Guid>.Failure(SchedulingErrors.AppointmentConflict);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class AdjustAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository appointmentRepository, IWorkOrderRepository workOrderRepository, IProfessionalRepository professionalRepository, IServiceRepository serviceRepository, AppointmentAvailability availability, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid appointmentId, AdjustAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        if (command.ProfessionalId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason) || command.Reason.Trim().Length > 1000) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);

        var appointment = await appointmentRepository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (appointment.Status != global::VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);

        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);

        var workOrder = command.WorkOrderId is { } workOrderId
            ? await workOrderRepository.GetForAccessAsync(workOrderId, scope.CompanyId, null, cancellationToken)
            : null;
        if (command.WorkOrderId is not null && workOrder is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);

        var serviceId = workOrder?.ServiceId ?? appointment.ServiceId;
        if (serviceId is { } selectedServiceId && professional.ProfessionalServices.All(item => item.ServiceId != selectedServiceId)) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);

        var durationMinutes = (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes;
        if (workOrder is not null)
        {
            var service = await serviceRepository.GetForCompanyAsync(workOrder.ServiceId, scope.CompanyId, cancellationToken);
            if (service is null || !service.IsActive) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
            durationMinutes = service.DurationMinutes;
        }
        await using var transaction = await appointmentRepository.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);
        if (!await availability.IsAvailableAsync(professional, command.Date, command.StartTime, durationMinutes, appointment.Id, cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, durationMinutes, out var startAtUtc, out var endAtUtc)) return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);

        var now = timeProvider.GetUtcNow();
        if (appointment.StartAtUtc != startAtUtc || appointment.EndAtUtc != endAtUtc)
        {
            appointment.Reschedule(startAtUtc, endAtUtc, command.Reason, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, now);
        }

        if (appointment.ProfessionalId != professional.Id)
        {
            appointment.Reassign(professional.Id, now);
        }

        if (appointment.WorkOrderId != command.WorkOrderId)
        {
            appointment.UpdateWorkOrderAssociation(
                workOrder?.Id,
                workOrder?.CustomerId ?? appointment.CustomerId,
                workOrder?.EquipmentId ?? appointment.EquipmentId,
                workOrder?.ServiceId ?? appointment.ServiceId,
                now);
        }

        if (await appointmentRepository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict) return OperationResult<Guid>.Failure(SchedulingErrors.AppointmentConflict);
        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class CreateScheduleBlockRequestUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(CreateScheduleBlockRequestCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (scope.ProfessionalId is not { } professionalId || scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        if (!IsValid(command)) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        var request = ScheduleBlockRequest.Create(scope.CompanyId, professionalId, command.Type, command.StartDate, command.EndDate, command.IsFullDay, command.StartTime, command.EndTime, command.Notes, timeProvider.GetUtcNow());
        if (TryGetRequestedInterval(request, professional.TimeZoneId, out var startAtUtc, out var endAtUtc) &&
            await repository.HasAppointmentConflictAsync(scope.CompanyId, professionalId, startAtUtc, endAtUtc, null, cancellationToken))
        {
            request.RequireAppointmentResolution();
        }
        await repository.AddBlockRequestAsync(request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(request.Id);
    }

    private static bool IsValid(CreateScheduleBlockRequestCommand command) =>
        Enum.IsDefined(command.Type) &&
        command.EndDate >= command.StartDate &&
        (command.Notes is null || command.Notes.Trim().Length <= 1000) &&
        (command.IsFullDay ||
         command.StartDate == command.EndDate &&
         command.StartTime is not null &&
         command.EndTime is not null &&
         command.StartTime < command.EndTime);

    private static bool TryGetRequestedInterval(ScheduleBlockRequest request, string timeZoneId, out DateTimeOffset startAtUtc, out DateTimeOffset endAtUtc)
    {
        var startTime = request.IsFullDay ? TimeOnly.MinValue : request.StartTime!.Value;
        var endDate = request.IsFullDay ? request.EndDate.AddDays(1) : request.EndDate;
        var endTime = request.IsFullDay ? TimeOnly.MinValue : request.EndTime!.Value;
        if (!AppointmentAvailability.TryGetUtcInterval(timeZoneId, request.StartDate, startTime, 1, out startAtUtc, out _))
        {
            endAtUtc = default;
            return false;
        }

        return AppointmentAvailability.TryGetUtcInterval(timeZoneId, endDate, endTime, 1, out endAtUtc, out _);
    }
}

public sealed class GetScheduleBlockRequestsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository)
{
    public async Task<IReadOnlyList<ScheduleBlockRequestDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        return await ExecuteAsync(scope, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleBlockRequestDto>> ExecuteAsync(WorkOrderAccessScope scope, CancellationToken cancellationToken = default)
    {
        var requests = await repository.GetBlockRequestsForAccessAsync(scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        var professionalIds = requests.Select(request => request.ProfessionalId).Distinct().ToArray();
        var professionalNames = await professionalRepository.GetDisplayNamesAsync(
            scope.CompanyId,
            professionalIds,
            cancellationToken);
        var timeZoneIds = await professionalRepository.GetTimeZoneIdsAsync(scope.CompanyId, professionalIds, cancellationToken);
        var result = new List<ScheduleBlockRequestDto>(requests.Count);
        foreach (var request in requests)
        {
            var affectedAppointments = Array.Empty<string>();
            if (request.Status == VoltaMarcada.Domain.Enums.ScheduleBlockRequestStatus.RequiresAppointmentResolution &&
                timeZoneIds.TryGetValue(request.ProfessionalId, out var timeZoneId) &&
                TryGetRequestedInterval(request, timeZoneId, out var startAtUtc, out var endAtUtc))
            {
                var appointments = await repository.GetListRangeForAccessAsync(scope.CompanyId, request.ProfessionalId, startAtUtc, endAtUtc, cancellationToken);
                affectedAppointments = appointments
                    .Where(item => item.Appointment.Status is VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled or VoltaMarcada.Domain.Enums.AppointmentStatus.InProgress)
                    .Select(item => FormatAffectedAppointment(item, timeZoneId))
                    .ToArray();
            }

            result.Add(new ScheduleBlockRequestDto(
            request.Id,
            request.ProfessionalId,
            professionalNames.GetValueOrDefault(request.ProfessionalId, string.Empty),
            request.Type,
            request.StartDate,
            request.EndDate,
            request.IsFullDay,
            request.StartTime,
            request.EndTime,
            request.Notes,
            request.Status,
            request.CreatedAtUtc,
            affectedAppointments));
        }

        return result;
    }

    private static bool TryGetRequestedInterval(ScheduleBlockRequest request, string timeZoneId, out DateTimeOffset startAtUtc, out DateTimeOffset endAtUtc)
    {
        var startTime = request.IsFullDay ? TimeOnly.MinValue : request.StartTime!.Value;
        var endDate = request.IsFullDay ? request.EndDate.AddDays(1) : request.EndDate;
        var endTime = request.IsFullDay ? TimeOnly.MinValue : request.EndTime!.Value;
        if (!AppointmentAvailability.TryGetUtcInterval(timeZoneId, request.StartDate, startTime, 1, out startAtUtc, out _))
        {
            endAtUtc = default;
            return false;
        }

        return AppointmentAvailability.TryGetUtcInterval(timeZoneId, endDate, endTime, 1, out endAtUtc, out _);
    }

    private static string FormatAffectedAppointment(AppointmentListItem item, string timeZoneId)
    {
        var localStart = TimeZoneInfo.ConvertTime(item.Appointment.StartAtUtc, TimeZoneInfo.FindSystemTimeZoneById(timeZoneId));
        return $"{localStart:dd/MM HH:mm} — {item.CustomerName ?? "Cliente não informado"}";
    }
}
