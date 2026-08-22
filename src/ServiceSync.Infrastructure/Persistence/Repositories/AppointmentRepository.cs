using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;

namespace ServiceSync.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository(AppDbContext dbContext) : IAppointmentRepository
{
    public async Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default) => await dbContext.Appointments.AddAsync(appointment, cancellationToken);

    public async Task AddBlockRequestAsync(ScheduleBlockRequest request, CancellationToken cancellationToken = default) => await dbContext.ScheduleBlockRequests.AddAsync(request, cancellationToken);

    public Task<Appointment?> GetForAccessAsync(Guid appointmentId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) =>
        dbContext.Appointments.Include(appointment => appointment.RescheduleHistory).SingleOrDefaultAsync(appointment => appointment.Id == appointmentId && appointment.CompanyId == companyId && (!professionalId.HasValue || appointment.ProfessionalId == professionalId.Value), cancellationToken);

    public Task<ScheduleBlockRequest?> GetBlockRequestForAccessAsync(Guid requestId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) =>
        dbContext.ScheduleBlockRequests.SingleOrDefaultAsync(request => request.Id == requestId && request.CompanyId == companyId && (!professionalId.HasValue || request.ProfessionalId == professionalId.Value), cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetRangeForAccessAsync(Guid companyId, Guid? professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default) =>
        await dbContext.Appointments.AsNoTracking().Where(appointment => appointment.CompanyId == companyId && (!professionalId.HasValue || appointment.ProfessionalId == professionalId.Value) && appointment.StartAtUtc < endAtUtc && appointment.EndAtUtc > startAtUtc).OrderBy(appointment => appointment.StartAtUtc).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<Appointment>> GetForWorkOrderAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) =>
        await dbContext.Appointments.AsNoTracking().Include(appointment => appointment.RescheduleHistory).Where(appointment => appointment.CompanyId == companyId && appointment.WorkOrderId == workOrderId && (!professionalId.HasValue || appointment.ProfessionalId == professionalId.Value)).OrderBy(appointment => appointment.StartAtUtc).ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<ScheduleBlockRequest>> GetBlockRequestsForAccessAsync(Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleBlockRequests.AsNoTracking().Where(request => request.CompanyId == companyId && (!professionalId.HasValue || request.ProfessionalId == professionalId.Value)).OrderByDescending(request => request.CreatedAtUtc).ToArrayAsync(cancellationToken);

    public Task<bool> HasAppointmentConflictAsync(Guid companyId, Guid professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, Guid? excludedAppointmentId, CancellationToken cancellationToken = default) =>
        dbContext.Appointments.AsNoTracking().AnyAsync(appointment => appointment.CompanyId == companyId && appointment.ProfessionalId == professionalId && (appointment.Status == AppointmentStatus.Scheduled || appointment.Status == AppointmentStatus.InProgress) && (!excludedAppointmentId.HasValue || appointment.Id != excludedAppointmentId.Value) && appointment.StartAtUtc < endAtUtc && appointment.EndAtUtc > startAtUtc, cancellationToken);

    public Task<bool> HasOpenAppointmentsAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default) =>
        dbContext.Appointments.AsNoTracking().AnyAsync(appointment => appointment.CompanyId == companyId && appointment.WorkOrderId == workOrderId && (appointment.Status == AppointmentStatus.Scheduled || appointment.Status == AppointmentStatus.InProgress), cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);
}
