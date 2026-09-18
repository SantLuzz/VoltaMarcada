using VoltaMarcada.Application.Abstractions.Operations;
using FluentValidation;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Scheduling;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.WorkOrders;

public sealed class CreateWorkOrderUseCase(IWorkOrderRepository workOrderRepository, ICustomerRepository customerRepository, IEquipmentRepository equipmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, IAppointmentRepository appointmentRepository, AppointmentAvailability appointmentAvailability, WorkOrderAuthorization authorization, ICurrentUser currentUser, IValidator<CreateWorkOrderCommand> validator, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(CreateWorkOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken); if (customer is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.CustomerNotFound);
        var firstLinkedItem = command.Items?.FirstOrDefault(item => item.EquipmentId is { } equipmentId && equipmentId != Guid.Empty && item.ServiceId is { } serviceId && serviceId != Guid.Empty);
        if (command.Items is not null && firstLinkedItem is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        var selectedEquipmentId = firstLinkedItem?.EquipmentId ?? command.EquipmentId;
        var selectedServiceId = firstLinkedItem?.ServiceId ?? command.ServiceId;
        var equipment = await equipmentRepository.GetForCompanyAsync(selectedEquipmentId, scope.CompanyId, cancellationToken); if (equipment is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.EquipmentNotFound); if (equipment.CustomerId != customer.Id) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidEquipmentCustomer);
        var service = await serviceRepository.GetForCompanyAsync(selectedServiceId, scope.CompanyId, cancellationToken); if (service is null || !service.IsActive) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ServiceNotFound);
        if (command.Items is not null)
        {
            foreach (var item in command.Items)
            {
                if (item.Type == WorkOrderItemType.Service && (item.ServiceId is null || item.ServiceId == Guid.Empty || item.EquipmentId is null || item.EquipmentId == Guid.Empty)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
                if (item.EquipmentId is { } equipmentId)
                {
                    var itemEquipment = await equipmentRepository.GetForCompanyAsync(equipmentId, scope.CompanyId, cancellationToken);
                    if (itemEquipment is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.EquipmentNotFound);
                    if (itemEquipment.CustomerId != customer.Id) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidEquipmentCustomer);
                }

                if (item.ServiceId is { } serviceId)
                {
                    var itemService = await serviceRepository.GetForCompanyAsync(serviceId, scope.CompanyId, cancellationToken);
                    if (itemService is null || !itemService.IsActive) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ServiceNotFound);
                }
            }
        }
        if (command.SourceAppointmentId is { } sourceAppointmentId)
        {
            var sourceAppointment = await appointmentRepository.GetForAccessAsync(sourceAppointmentId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId, cancellationToken);
            if (sourceAppointment is null ||
                sourceAppointment.Status != AppointmentStatus.Completed ||
                sourceAppointment.Type is not (AppointmentType.Visit or AppointmentType.Other) ||
                sourceAppointment.CustomerId != customer.Id ||
                sourceAppointment.EquipmentId is { } sourceEquipmentId && sourceEquipmentId != equipment.Id)
            {
                return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
            }
        }
        Guid? professionalId = command.ProfessionalId;
        if (!scope.IsOwner) { if (scope.ProfessionalId is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalProfileRequired); professionalId = scope.ProfessionalId; }
        global::VoltaMarcada.Domain.Entities.Professional? professional = null;
        if (professionalId is { } id) { professional = await professionalRepository.GetForCompanyAsync(id, scope.CompanyId, cancellationToken); if (professional is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotFound); if (!professional.IsActive || professional.ProfessionalServices.All(x => x.ServiceId != service.Id)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalNotQualified); }
        DateTimeOffset appointmentStartAtUtc = default;
        DateTimeOffset appointmentEndAtUtc = default;
        if (command.Appointment is not null)
        {
            if (professional is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalRequired);
        }

        await using var schedulingTransaction = command.Appointment is null
            ? null
            : await appointmentRepository.BeginSchedulingTransactionAsync(scope.CompanyId, professional!.Id, cancellationToken);
        if (command.Appointment is not null)
        {
            if (!await appointmentAvailability.IsAvailableAsync(professional!, command.Appointment.Date, command.Appointment.StartTime, service.DurationMinutes, cancellationToken: cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional!.TimeZoneId, command.Appointment.Date, command.Appointment.StartTime, service.DurationMinutes, out appointmentStartAtUtc, out appointmentEndAtUtc)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalUnavailable);
        }
        IReadOnlyCollection<WorkOrderItemDefinition> items;
        if (command.Items is not null)
        {
            if (!WorkOrderFinancialItems.TryCreate(command.Items, out items)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        }
        else if (!WorkOrderFinancialItems.TryCreate(service, command.ServiceUnitPrice, command.AdditionalItems, out items))
        {
            return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        }
        var now = timeProvider.GetUtcNow();
        var workOrder = global::VoltaMarcada.Domain.Entities.WorkOrder.Create(scope.CompanyId, customer.Id, equipment.Id, service.Id, professionalId, currentUser.UserId, command.Priority, command.ReportedProblem, items, service.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder)).ToArray(), now, discountAmount: command.DiscountAmount, sourceAppointmentId: command.SourceAppointmentId);
        workOrder.RecordHistory(WorkOrderHistoryEventType.Created, currentUser.UserId == Guid.Empty ? null : currentUser.UserId, WorkOrderHistoryDescriptions.Created(), now);
        await workOrderRepository.AddAsync(workOrder, cancellationToken);
        if (command.Appointment is not null)
        {
            var appointment = global::VoltaMarcada.Domain.Entities.Appointment.Create(scope.CompanyId, professional!.Id, customer.Id, equipment.Id, service.Id, workOrder.Id, appointmentStartAtUtc, appointmentEndAtUtc, null, now, type: AppointmentType.Service);
            await appointmentRepository.AddAsync(appointment, cancellationToken);
        }

        if (command.Appointment is not null)
        {
            if (await appointmentRepository.TrySaveAppointmentChangesAsync(cancellationToken) == AppointmentPersistenceOutcome.Conflict)
            {
                return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalUnavailable);
            }

            await schedulingTransaction!.CommitAsync(cancellationToken);
        }
        else
        {
            await workOrderRepository.SaveChangesAsync(cancellationToken);
        }

        return OperationResult<WorkOrderOperationValue>.Success(new WorkOrderOperationValue(workOrder.Id));
    }
}

