using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.WorkOrders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Scheduling;

public sealed class RescheduleAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository, AppointmentAvailability availability, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid appointmentId, RescheduleAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        var professional = await professionalRepository.GetForCompanyAsync(appointment.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        var duration = (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes;
        if (!await availability.IsAvailableAsync(professional, command.Date, command.StartTime, duration, appointment.Id, cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, duration, out var startAtUtc, out var endAtUtc)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.ProfessionalUnavailable);
        if (string.IsNullOrWhiteSpace(command.Reason)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidInput);
        appointment.Reschedule(startAtUtc, endAtUtc, command.Reason, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}

public sealed class ReassignAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository, AppointmentAvailability availability, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid appointmentId, Guid professionalId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (appointment is null || professional is null || !professional.IsActive) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        var localStart = TimeZoneInfo.ConvertTime(appointment.StartAtUtc, TimeZoneInfo.FindSystemTimeZoneById(professional.TimeZoneId));
        var duration = (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes;
        if (!await availability.IsAvailableAsync(professional, DateOnly.FromDateTime(localStart.Date), TimeOnly.FromDateTime(localStart.DateTime), duration, appointment.Id, cancellationToken)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.ProfessionalUnavailable);
        appointment.Reassign(professionalId, timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}

public sealed class AdjustAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository appointmentRepository, IWorkOrderRepository workOrderRepository, IProfessionalRepository professionalRepository, IServiceRepository serviceRepository, AppointmentAvailability availability, ICurrentUser currentUser, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid appointmentId, AdjustAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        if (command.ProfessionalId == Guid.Empty || string.IsNullOrWhiteSpace(command.Reason)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidInput);

        var appointment = await appointmentRepository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        if (appointment.Status != global::ServiceSync.Domain.Enums.AppointmentStatus.Scheduled) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidStatus);

        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);

        var workOrder = command.WorkOrderId is { } workOrderId
            ? await workOrderRepository.GetForAccessAsync(workOrderId, scope.CompanyId, null, cancellationToken)
            : null;
        if (command.WorkOrderId is not null && workOrder is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);

        var serviceId = workOrder?.ServiceId ?? appointment.ServiceId;
        if (serviceId is { } selectedServiceId && professional.ProfessionalServices.All(item => item.ServiceId != selectedServiceId)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);

        var durationMinutes = (int)(appointment.EndAtUtc - appointment.StartAtUtc).TotalMinutes;
        if (workOrder is not null)
        {
            var service = await serviceRepository.GetForCompanyAsync(workOrder.ServiceId, scope.CompanyId, cancellationToken);
            if (service is null || !service.IsActive) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
            durationMinutes = service.DurationMinutes;
        }
        if (!await availability.IsAvailableAsync(professional, command.Date, command.StartTime, durationMinutes, appointment.Id, cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Date, command.StartTime, durationMinutes, out var startAtUtc, out var endAtUtc)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.ProfessionalUnavailable);

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

        await appointmentRepository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}

public sealed class CreateScheduleBlockRequestUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(CreateScheduleBlockRequestCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (scope.ProfessionalId is not { } professionalId || scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        var request = ScheduleBlockRequest.Create(scope.CompanyId, professionalId, command.Type, command.StartDate, command.EndDate, command.IsFullDay, command.StartTime, command.EndTime, command.Notes, timeProvider.GetUtcNow());
        await repository.AddBlockRequestAsync(request, cancellationToken);
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(request.Id);
    }
}

public sealed class GetScheduleBlockRequestsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository)
{
    public async Task<IReadOnlyList<ScheduleBlockRequestDto>> ExecuteAsync(CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        return await ExecuteAsync(scope, cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleBlockRequestDto>> ExecuteAsync(WorkOrderAccessScope scope, CancellationToken cancellationToken = default)
    {
        var requests = await repository.GetBlockRequestsForAccessAsync(scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        return requests.Select(request => new ScheduleBlockRequestDto(request.Id, request.ProfessionalId, request.Type, request.StartDate, request.EndDate, request.IsFullDay, request.StartTime, request.EndTime, request.Notes, request.Status, request.CreatedAtUtc)).ToArray();
    }
}
