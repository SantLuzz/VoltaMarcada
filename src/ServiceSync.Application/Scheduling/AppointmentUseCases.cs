using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.WorkOrders;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Scheduling;

public sealed class GetAvailableAppointmentSlotsUseCase(WorkOrderAuthorization authorization, IProfessionalRepository professionalRepository, AppointmentAvailability availability, ISchedulingConfiguration configuration)
{
    public async Task<IReadOnlyList<AppointmentSlotDto>> ExecuteAsync(Guid professionalId, DateOnly date, int durationMinutes, Guid? excludedAppointmentId = null, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner && scope.ProfessionalId != professionalId)
        {
            return [];
        }

        var professional = await professionalRepository.GetForCompanyAsync(professionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive || durationMinutes <= 0)
        {
            return [];
        }

        var slots = new List<AppointmentSlotDto>();
        for (var minute = 0; minute < 24 * 60;)
        {
            var time = new TimeOnly(minute / 60, minute % 60);
            if (await availability.IsAvailableAsync(professional, date, time, durationMinutes, excludedAppointmentId, cancellationToken))
            {
                slots.Add(new AppointmentSlotDto(time));
                minute = GetNextSlotMinute(minute + durationMinutes, configuration.SlotIntervalMinutes);
                continue;
            }

            minute += configuration.SlotIntervalMinutes;
        }

        return slots;
    }

    private static int GetNextSlotMinute(int minute, int slotIntervalMinutes)
    {
        var remainder = minute % slotIntervalMinutes;
        return remainder == 0 ? minute : minute + slotIntervalMinutes - remainder;
    }
}

public sealed class CreateAppointmentUseCase(WorkOrderAuthorization authorization, IProfessionalRepository professionalRepository, IWorkOrderRepository workOrderRepository, ICustomerRepository customerRepository, IEquipmentRepository equipmentRepository, IServiceRepository serviceRepository, IAppointmentRepository appointmentRepository, AppointmentAvailability availability, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(CreateAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive)
        {
            return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        }

        var normalized = command;
        if (!scope.IsOwner)
        {
            if (scope.ProfessionalId != professional.Id || command.WorkOrderId is not { } workOrderId)
            {
                return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
            }

            var workOrder = await workOrderRepository.GetForAccessAsync(workOrderId, scope.CompanyId, professional.Id, cancellationToken);
            if (workOrder is null || workOrder.ProfessionalId != professional.Id)
            {
                return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
            }

            normalized = command with { CustomerId = workOrder.CustomerId, EquipmentId = workOrder.EquipmentId, ServiceId = workOrder.ServiceId, ProfessionalId = professional.Id };
        }
        else
        {
            if (normalized.WorkOrderId is { } workOrderId)
            {
                var workOrder = await workOrderRepository.GetForAccessAsync(workOrderId, scope.CompanyId, null, cancellationToken);
                if (workOrder is null)
                {
                    return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
                }

                normalized = command with
                {
                    CustomerId = workOrder.CustomerId,
                    EquipmentId = workOrder.EquipmentId,
                    ServiceId = workOrder.ServiceId
                };
            }

            if (normalized.CustomerId is { } customerId && await customerRepository.GetForCompanyAsync(customerId, scope.CompanyId, cancellationToken) is null)
            {
                return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
            }

            if (normalized.EquipmentId is { } equipmentId)
            {
                var equipment = await equipmentRepository.GetForCompanyAsync(equipmentId, scope.CompanyId, cancellationToken);
                if (equipment is null || normalized.CustomerId is { } equipmentCustomerId && equipment.CustomerId != equipmentCustomerId)
                {
                    return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
                }
            }

            if (normalized.ServiceId is { } serviceId && await serviceRepository.GetForCompanyAsync(serviceId, scope.CompanyId, cancellationToken) is null)
            {
                return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
            }
        }

        if (!await availability.IsAvailableAsync(professional, normalized.Date, normalized.StartTime, normalized.DurationMinutes, cancellationToken: cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, normalized.Date, normalized.StartTime, normalized.DurationMinutes, out var startAtUtc, out var endAtUtc))
        {
            return SchedulingOperationResult.Failure(SchedulingOperationResultCode.ProfessionalUnavailable);
        }

        var appointment = Appointment.Create(scope.CompanyId, professional.Id, normalized.CustomerId, normalized.EquipmentId, normalized.ServiceId, normalized.WorkOrderId, startAtUtc, endAtUtc, normalized.Notes, timeProvider.GetUtcNow());
        await appointmentRepository.AddAsync(appointment, cancellationToken);
        await appointmentRepository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}

public sealed class GetAppointmentsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository)
{
    public async Task<IReadOnlyList<AppointmentDto>> ExecuteAsync(DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        return await ExecuteAsync(scope, startAtUtc, endAtUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDto>> ExecuteAsync(WorkOrderAccessScope scope, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default)
    {
        var items = await repository.GetRangeForAccessAsync(scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, startAtUtc, endAtUtc, cancellationToken);
        return items.Select(item => new AppointmentDto(item.Id, item.ProfessionalId, item.CustomerId, item.EquipmentId, item.ServiceId, item.WorkOrderId, item.StartAtUtc, item.EndAtUtc, item.Status, item.Notes)).ToArray();
    }
}

public sealed class GetAppointmentDetailsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository appointmentRepository, ICustomerRepository customerRepository)
{
    public async Task<AppointmentDetailsDto?> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointment = await appointmentRepository.GetForAccessAsync(
            appointmentId,
            scope.CompanyId,
            scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty,
            cancellationToken);
        if (appointment is null)
        {
            return null;
        }

        AppointmentCustomerDetailsDto? customer = null;
        if (appointment.CustomerId is { } customerId)
        {
            var customerEntity = await customerRepository.GetForCompanyAsync(customerId, scope.CompanyId, cancellationToken);
            if (customerEntity is not null)
            {
                var address = customerEntity.Addresses.SingleOrDefault(item => item.IsPrimary) ?? customerEntity.Addresses.FirstOrDefault();
                customer = new AppointmentCustomerDetailsDto(
                    customerEntity.Id,
                    customerEntity.Name,
                    customerEntity.WhatsApp,
                    customerEntity.Email,
                    address is null
                        ? null
                        : $"{address.Street}, {address.Number} - {address.City.Name}/{address.State.Abbreviation}");
            }
        }

        return new AppointmentDetailsDto(
            appointment.Id,
            appointment.ProfessionalId,
            appointment.WorkOrderId,
            appointment.StartAtUtc,
            appointment.EndAtUtc,
            appointment.Status,
            appointment.Notes,
            customer);
    }
}

public sealed class CompleteAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (appointment is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        if (appointment.Status != ServiceSync.Domain.Enums.AppointmentStatus.InProgress) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidStatus);
        appointment.Complete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}

public sealed class StartAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (appointment is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        if (appointment.Status != ServiceSync.Domain.Enums.AppointmentStatus.Scheduled) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidStatus);
        appointment.Start(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}

public sealed class CancelAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<SchedulingOperationResult> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.Forbidden);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.NotFound);
        if (appointment.Status is not (ServiceSync.Domain.Enums.AppointmentStatus.Scheduled or ServiceSync.Domain.Enums.AppointmentStatus.InProgress)) return SchedulingOperationResult.Failure(SchedulingOperationResultCode.InvalidStatus);
        appointment.Cancel(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return SchedulingOperationResult.Success(appointment.Id);
    }
}
