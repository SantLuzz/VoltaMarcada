using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.WorkOrders;

namespace ServiceSync.Application.Scheduling;

public sealed class GetWorkOrderAppointmentsUseCase(WorkOrderAuthorization authorization, IAppointmentRepository repository)
{
    public async Task<IReadOnlyList<AppointmentDto>> ExecuteAsync(Guid workOrderId, CancellationToken cancellationToken = default)
    {
        var scope = await authorization.GetAccessScopeAsync(cancellationToken);
        var appointments = await repository.GetForWorkOrderAsync(workOrderId, scope.CompanyId, scope.IsOwner ? null : scope.ProfessionalId ?? Guid.Empty, cancellationToken);
        return appointments.Select(appointment => new AppointmentDto(appointment.Id, appointment.ProfessionalId, appointment.CustomerId, appointment.EquipmentId, appointment.ServiceId, appointment.WorkOrderId, appointment.StartAtUtc, appointment.EndAtUtc, appointment.Status, appointment.Notes)).ToArray();
    }
}
