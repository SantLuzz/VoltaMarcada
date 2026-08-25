using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Npgsql;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class AppointmentRepository(AppDbContext dbContext) : IAppointmentRepository
{
    public async Task<IAppointmentSchedulingTransaction> BeginSchedulingTransactionAsync(Guid companyId, Guid professionalId, CancellationToken cancellationToken = default)
    {
        var transaction = await dbContext.Database.BeginTransactionAsync(System.Data.IsolationLevel.Serializable, cancellationToken);
        var lockKey = $"{companyId:N}:{professionalId:N}";
        await dbContext.Database.ExecuteSqlInterpolatedAsync(
            $"SELECT pg_advisory_xact_lock(hashtextextended({lockKey}, 0));",
            cancellationToken);
        return new AppointmentSchedulingTransaction(transaction);
    }

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

    public async Task<IReadOnlyList<AppointmentListItem>> GetListRangeForAccessAsync(
        Guid companyId,
        Guid? professionalId,
        DateTimeOffset startAtUtc,
        DateTimeOffset endAtUtc,
        CancellationToken cancellationToken = default) =>
        await GetListQuery(companyId, professionalId, startAtUtc, endAtUtc, null)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<AppointmentListItem>> GetListForWorkOrderAsync(
        Guid workOrderId,
        Guid companyId,
        Guid? professionalId,
        CancellationToken cancellationToken = default) =>
        await GetListQuery(companyId, professionalId, null, null, workOrderId)
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<ScheduleBlockRequest>> GetBlockRequestsForAccessAsync(Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleBlockRequests.AsNoTracking().Where(request => request.CompanyId == companyId && (!professionalId.HasValue || request.ProfessionalId == professionalId.Value)).OrderByDescending(request => request.CreatedAtUtc).ToArrayAsync(cancellationToken);

    public Task<bool> HasAppointmentConflictAsync(Guid companyId, Guid professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, Guid? excludedAppointmentId, CancellationToken cancellationToken = default) =>
        dbContext.Appointments.AsNoTracking().AnyAsync(appointment => appointment.CompanyId == companyId && appointment.ProfessionalId == professionalId && (appointment.Status == AppointmentStatus.Scheduled || appointment.Status == AppointmentStatus.InProgress) && (!excludedAppointmentId.HasValue || appointment.Id != excludedAppointmentId.Value) && appointment.StartAtUtc < endAtUtc && appointment.EndAtUtc > startAtUtc, cancellationToken);

    public Task<bool> HasOpenAppointmentsAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default) =>
        dbContext.Appointments.AsNoTracking().AnyAsync(appointment => appointment.CompanyId == companyId && appointment.WorkOrderId == workOrderId && (appointment.Status == AppointmentStatus.Scheduled || appointment.Status == AppointmentStatus.InProgress), cancellationToken);

    public async Task<AppointmentPersistenceOutcome> TrySaveAppointmentChangesAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
            return AppointmentPersistenceOutcome.Saved;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException &&
                                                 postgresException.SqlState == PostgresErrorCodes.ExclusionViolation &&
                                                 postgresException.ConstraintName == "EX_Appointments_OpenProfessionalInterval")
        {
            dbContext.ChangeTracker.Clear();
            return AppointmentPersistenceOutcome.Conflict;
        }
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

    private IQueryable<AppointmentListItem> GetListQuery(
        Guid companyId,
        Guid? professionalId,
        DateTimeOffset? startAtUtc,
        DateTimeOffset? endAtUtc,
        Guid? workOrderId)
    {
        var appointments = dbContext.Appointments.AsNoTracking()
            .Where(appointment => appointment.CompanyId == companyId);

        if (professionalId is { } selectedProfessionalId)
        {
            appointments = appointments.Where(appointment => appointment.ProfessionalId == selectedProfessionalId);
        }

        if (startAtUtc is { } start && endAtUtc is { } end)
        {
            appointments = appointments.Where(appointment => appointment.StartAtUtc < end && appointment.EndAtUtc > start);
        }

        if (workOrderId is { } selectedWorkOrderId)
        {
            appointments = appointments.Where(appointment => appointment.WorkOrderId == selectedWorkOrderId);
        }

        appointments = appointments.OrderBy(appointment => appointment.StartAtUtc);

        return
            from appointment in appointments
            join professional in dbContext.Professionals.AsNoTracking().Where(item => item.CompanyId == companyId)
                on appointment.ProfessionalId equals professional.Id
            join member in dbContext.CompanyUsers.AsNoTracking().Where(item => item.CompanyId == companyId)
                on professional.CompanyUserId equals member.Id
            join user in dbContext.Users.AsNoTracking()
                on member.UserId equals user.Id
            join customer in dbContext.Customers.AsNoTracking().Where(item => item.CompanyId == companyId)
                on appointment.CustomerId equals (Guid?)customer.Id into customers
            from customer in customers.DefaultIfEmpty()
            join equipment in dbContext.Equipment.AsNoTracking().Where(item => item.CompanyId == companyId)
                on appointment.EquipmentId equals (Guid?)equipment.Id into equipmentItems
            from equipment in equipmentItems.DefaultIfEmpty()
            join service in dbContext.Services.AsNoTracking().Where(item => item.CompanyId == companyId)
                on appointment.ServiceId equals (Guid?)service.Id into services
            from service in services.DefaultIfEmpty()
            select new AppointmentListItem(
                appointment,
                user.FullName ?? user.Email ?? string.Empty,
                customer == null ? null : customer.Name,
                equipment == null
                    ? null
                    : equipment.Type +
                      (equipment.Manufacturer == null || equipment.Manufacturer == string.Empty ? string.Empty : " " + equipment.Manufacturer) +
                      (equipment.Model == null || equipment.Model == string.Empty ? string.Empty : " " + equipment.Model),
                service == null ? null : service.Name);
    }

    private sealed class AppointmentSchedulingTransaction(IDbContextTransaction transaction) : IAppointmentSchedulingTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => transaction.CommitAsync(cancellationToken);

        public ValueTask DisposeAsync() => transaction.DisposeAsync();
    }
}
