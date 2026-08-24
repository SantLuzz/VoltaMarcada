using ServiceSync.Application.Abstractions.Operations;
using FluentValidation;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Scheduling;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.WorkOrders;

public sealed class CreateWorkOrderUseCase(IWorkOrderRepository workOrderRepository, ICustomerRepository customerRepository, IEquipmentRepository equipmentRepository, IServiceRepository serviceRepository, IProfessionalRepository professionalRepository, IAppointmentRepository appointmentRepository, AppointmentAvailability appointmentAvailability, WorkOrderAuthorization authorization, ICurrentUser currentUser, IValidator<CreateWorkOrderCommand> validator, TimeProvider timeProvider)
{
    public async Task<OperationResult<WorkOrderOperationValue>> ExecuteAsync(CreateWorkOrderCommand command, CancellationToken cancellationToken = default)
    {
        if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken); if (customer is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.CustomerNotFound);
        var equipment = await equipmentRepository.GetForCompanyAsync(command.EquipmentId, scope.CompanyId, cancellationToken); if (equipment is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.EquipmentNotFound); if (equipment.CustomerId != customer.Id) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidEquipmentCustomer);
        var service = await serviceRepository.GetForCompanyAsync(command.ServiceId, scope.CompanyId, cancellationToken); if (service is null || !service.IsActive) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ServiceNotFound);
        Guid? professionalId = command.ProfessionalId;
        if (!scope.IsOwner) { if (scope.ProfessionalId is null) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalProfileRequired); professionalId = scope.ProfessionalId; }
        global::ServiceSync.Domain.Entities.Professional? professional = null;
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
            if (!await appointmentAvailability.IsAvailableAsync(professional, command.Appointment.Date, command.Appointment.StartTime, service.DurationMinutes, cancellationToken: cancellationToken) || !AppointmentAvailability.TryGetUtcInterval(professional.TimeZoneId, command.Appointment.Date, command.Appointment.StartTime, service.DurationMinutes, out appointmentStartAtUtc, out appointmentEndAtUtc)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.ProfessionalUnavailable);
        }
        if (!WorkOrderFinancialItems.TryCreate(service, command.ServiceUnitPrice, command.AdditionalItems, out var items)) return OperationResult<WorkOrderOperationValue>.Failure(WorkOrderErrors.InvalidInput);
        var now = timeProvider.GetUtcNow();
        var workOrder = global::ServiceSync.Domain.Entities.WorkOrder.Create(scope.CompanyId, customer.Id, equipment.Id, service.Id, professionalId, currentUser.UserId, command.Priority, command.ReportedProblem, items, service.ChecklistItems.OrderBy(x => x.DisplayOrder).Select(x => new WorkOrderChecklistItemDefinition(x.Description, x.IsRequired, x.DisplayOrder)).ToArray(), now);
        await workOrderRepository.AddAsync(workOrder, cancellationToken);
        if (command.Appointment is not null)
        {
            var appointment = global::ServiceSync.Domain.Entities.Appointment.Create(scope.CompanyId, professional!.Id, customer.Id, equipment.Id, service.Id, workOrder.Id, appointmentStartAtUtc, appointmentEndAtUtc, null, now);
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
