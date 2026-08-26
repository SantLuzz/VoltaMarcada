using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;
using VoltaMarcada.Application.MaintenanceOpportunities;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Application.Scheduling;

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

public sealed class CreateAppointmentUseCase(WorkOrderAuthorization authorization, IProfessionalRepository professionalRepository, IWorkOrderRepository workOrderRepository, ICustomerRepository customerRepository, IEquipmentRepository equipmentRepository, IServiceRepository serviceRepository, IAppointmentRepository appointmentRepository, IMaintenanceOpportunityRepository maintenanceOpportunityRepository, AppointmentAvailability availability, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(CreateAppointmentCommand command, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (command.ProfessionalId == Guid.Empty ||
            command.DurationMinutes <= 0 ||
            command.Notes?.Trim().Length > 1000 ||
            command.Description?.Trim().Length > 1000 ||
            !Enum.IsDefined(command.Type) ||
            command.Type == global::VoltaMarcada.Domain.Enums.AppointmentType.ServiceExecution && command.WorkOrderId is null)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.InvalidInput);
        }

        var professional = await professionalRepository.GetForCompanyAsync(command.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null || !professional.IsActive)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        }

        var normalized = command;
        MaintenanceOpportunity? maintenanceOpportunity = null;
        if (command.MaintenanceOpportunityId is { } maintenanceOpportunityId)
        {
            if (!scope.IsOwner)
            {
                return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
            }

            maintenanceOpportunity = await maintenanceOpportunityRepository.GetForCompanyAsync(maintenanceOpportunityId, scope.CompanyId, cancellationToken);
            if (maintenanceOpportunity is null)
            {
                return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
            }

            if (maintenanceOpportunity.Status != MaintenanceOpportunityStatus.BookingRequested)
            {
                return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
            }

            normalized = command with
            {
                CustomerId = maintenanceOpportunity.CustomerId,
                EquipmentId = maintenanceOpportunity.EquipmentId,
                ServiceId = maintenanceOpportunity.RecommendedServiceId,
                WorkOrderId = null
            };
        }
        if (!scope.IsOwner)
        {
            if (scope.ProfessionalId != professional.Id || command.WorkOrderId is not { } workOrderId)
            {
                return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
            }

            var workOrder = await workOrderRepository.GetForAccessAsync(workOrderId, scope.CompanyId, professional.Id, cancellationToken);
            if (workOrder is null || workOrder.ProfessionalId != professional.Id)
            {
                return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
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
                    return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
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
                return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
            }

            if (normalized.EquipmentId is { } equipmentId)
            {
                var equipment = await equipmentRepository.GetForCompanyAsync(equipmentId, scope.CompanyId, cancellationToken);
                if (equipment is null || normalized.CustomerId is { } equipmentCustomerId && equipment.CustomerId != equipmentCustomerId)
                {
                    return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
                }
            }

            if (normalized.ServiceId is { } serviceId && await serviceRepository.GetForCompanyAsync(serviceId, scope.CompanyId, cancellationToken) is null)
            {
                return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
            }
        }

        if (normalized.ServiceId is { } selectedServiceId &&
            professional.ProfessionalServices.All(item => item.ServiceId != selectedServiceId))
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalNotQualified);
        }

        await using var transaction = await appointmentRepository.BeginSchedulingTransactionAsync(scope.CompanyId, professional.Id, cancellationToken);
        if (!await availability.IsAvailableAsync(professional, normalized.Date, normalized.StartTime, normalized.DurationMinutes, cancellationToken: cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, normalized.Date, normalized.StartTime, normalized.DurationMinutes, out var startAtUtc, out var endAtUtc))
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.ProfessionalUnavailable);
        }

        var appointment = Appointment.Create(scope.CompanyId, professional.Id, normalized.CustomerId, normalized.EquipmentId, normalized.ServiceId, normalized.WorkOrderId, startAtUtc, endAtUtc, normalized.Notes, timeProvider.GetUtcNow(), AppointmentOrigin.Internal, normalized.Type, normalized.Description);
        await appointmentRepository.AddAsync(appointment, cancellationToken);
        maintenanceOpportunity?.MarkAsBooked(timeProvider.GetUtcNow());
        if (await appointmentRepository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict)
        {
            return OperationResult<Guid>.Failure(SchedulingErrors.AppointmentConflict);
        }

        await transaction.CommitAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class GetAppointmentsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository)
{
    public async Task<IReadOnlyList<AppointmentDto>> ExecuteAsync(DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        return await ExecuteAsync(scope, startAtUtc, endAtUtc, cancellationToken);
    }

    public async Task<IReadOnlyList<AppointmentDto>> ExecuteAsync(WorkOrderAccessScope scope, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default)
    {
        var items = await repository.GetListRangeForAccessAsync(scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, startAtUtc, endAtUtc, cancellationToken);
        var timeZoneIds = await professionalRepository.GetTimeZoneIdsAsync(scope.CompanyId, items.Select(item => item.Appointment.ProfessionalId).Distinct().ToArray(), cancellationToken);
        return items.Select(item => new AppointmentDto(
            item.Appointment.Id,
            item.Appointment.ProfessionalId,
            item.Appointment.CustomerId,
            item.Appointment.EquipmentId,
            item.Appointment.ServiceId,
            item.Appointment.WorkOrderId,
            item.Appointment.StartAtUtc,
            item.Appointment.EndAtUtc,
            item.Appointment.Status,
            item.Appointment.Origin,
            item.Appointment.Type,
            item.Appointment.Description,
            item.Appointment.Notes,
            timeZoneIds[item.Appointment.ProfessionalId],
            item.ProfessionalName,
            item.CustomerName,
            item.EquipmentName,
            item.ServiceName,
            [])).ToArray();
    }
}

public sealed class GetAppointmentDetailsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository appointmentRepository, ICustomerRepository customerRepository, IProfessionalRepository professionalRepository)
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

        var professional = await professionalRepository.GetForCompanyAsync(appointment.ProfessionalId, scope.CompanyId, cancellationToken);
        if (professional is null)
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
            appointment.Origin,
            appointment.Type,
            appointment.Description,
            appointment.Notes,
            customer,
            professional.TimeZoneId);
    }
}

public sealed class CompleteAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (appointment is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (appointment.Status != VoltaMarcada.Domain.Enums.AppointmentStatus.InProgress) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        appointment.Complete(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class StartAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        if (appointment is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (appointment.Status != VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        appointment.Start(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}

public sealed class CancelAppointmentUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(Guid appointmentId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        if (!scope.IsOwner) return OperationResult<Guid>.Failure(SchedulingErrors.Forbidden);
        var appointment = await repository.GetForAccessAsync(appointmentId, scope.CompanyId, null, cancellationToken);
        if (appointment is null) return OperationResult<Guid>.Failure(SchedulingErrors.NotFound);
        if (appointment.Status is not (VoltaMarcada.Domain.Enums.AppointmentStatus.Scheduled or VoltaMarcada.Domain.Enums.AppointmentStatus.InProgress)) return OperationResult<Guid>.Failure(SchedulingErrors.InvalidStatus);
        appointment.Cancel(timeProvider.GetUtcNow());
        await repository.SaveChangesAsync(cancellationToken);
        return OperationResult<Guid>.Success(appointment.Id);
    }
}
