using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IAppointmentRepository
{
    Task<IAppointmentSchedulingTransaction> BeginSchedulingTransactionAsync(Guid companyId, Guid professionalId, CancellationToken cancellationToken = default);
    Task AddAsync(Appointment appointment, CancellationToken cancellationToken = default);
    Task AddBlockRequestAsync(ScheduleBlockRequest request, CancellationToken cancellationToken = default);
    Task<Appointment?> GetForAccessAsync(Guid appointmentId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<ScheduleBlockRequest?> GetBlockRequestForAccessAsync(Guid requestId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetRangeForAccessAsync(Guid companyId, Guid? professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Appointment>> GetForWorkOrderAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentListItem>> GetListRangeForAccessAsync(Guid companyId, Guid? professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<AppointmentListItem>> GetListForWorkOrderAsync(Guid workOrderId, Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<ScheduleBlockRequest>> GetBlockRequestsForAccessAsync(Guid companyId, Guid? professionalId, CancellationToken cancellationToken = default);
    Task<bool> HasAppointmentConflictAsync(Guid companyId, Guid professionalId, DateTimeOffset startAtUtc, DateTimeOffset endAtUtc, Guid? excludedAppointmentId, CancellationToken cancellationToken = default);
    Task<bool> HasOpenAppointmentsAsync(Guid companyId, Guid workOrderId, CancellationToken cancellationToken = default);
    Task<AppointmentPersistenceOutcome> TrySaveAppointmentChangesAsync(CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IAppointmentSchedulingTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken = default);
}

public enum AppointmentPersistenceOutcome
{
    Saved,
    Conflict
}

public sealed record AppointmentListItem(
    Appointment Appointment,
    string ProfessionalName,
    string? CustomerName,
    string? EquipmentName,
    string? ServiceName);
