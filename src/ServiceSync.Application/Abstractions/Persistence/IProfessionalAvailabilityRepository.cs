using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IProfessionalAvailabilityRepository
{
    Task<bool> ProfessionalExistsAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default);

    Task<ProfessionalAvailability?> GetAvailabilityAsync(
        Guid professionalId,
        Guid companyId,
        bool trackChanges,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleBlock>> GetScheduleBlocksAsync(
        Guid professionalId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task<ScheduleBlock?> GetScheduleBlockAsync(
        Guid scheduleBlockId,
        Guid professionalId,
        Guid companyId,
        CancellationToken cancellationToken = default);

    Task AddAvailabilityAsync(ProfessionalAvailability availability, CancellationToken cancellationToken = default);

    Task AddScheduleBlockAsync(ScheduleBlock scheduleBlock, CancellationToken cancellationToken = default);

    void RemoveScheduleBlock(ScheduleBlock scheduleBlock);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
