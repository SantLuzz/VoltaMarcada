using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.WorkOrders;

namespace VoltaMarcada.Application.Scheduling;

public sealed class GetWorkOrderAppointmentsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository, IProfessionalRepository professionalRepository)
{
    public async Task<IReadOnlyList<AppointmentDto>> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointments = await repository.GetListForWorkOrderAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        var appointmentHistories = await repository.GetForWorkOrderAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        var historiesByAppointmentId = appointmentHistories.ToDictionary(
            appointment => appointment.Id,
            appointment => (IReadOnlyList<AppointmentRescheduleHistoryDto>)appointment.RescheduleHistory
                .OrderByDescending(history => history.CreatedAtUtc)
                .Select(history => new AppointmentRescheduleHistoryDto(
                    history.PreviousStartAtUtc,
                    history.PreviousEndAtUtc,
                    history.NewStartAtUtc,
                    history.NewEndAtUtc,
                    history.Reason,
                    history.CreatedAtUtc))
                .ToArray());
        var timeZoneIds = await professionalRepository.GetTimeZoneIdsAsync(scope.CompanyId, appointments.Select(item => item.Appointment.ProfessionalId).Distinct().ToArray(), cancellationToken);
        return appointments.Select(item => new AppointmentDto(
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
            item.Appointment.Notes,
            timeZoneIds[item.Appointment.ProfessionalId],
            item.ProfessionalName,
            item.CustomerName,
            item.EquipmentName,
            item.ServiceName,
            historiesByAppointmentId.GetValueOrDefault(item.Appointment.Id, []))).ToArray();
    }
}
