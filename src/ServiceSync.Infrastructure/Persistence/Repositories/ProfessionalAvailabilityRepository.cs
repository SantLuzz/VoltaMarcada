using Microsoft.EntityFrameworkCore;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Infrastructure.Persistence.Repositories;

public sealed class ProfessionalAvailabilityRepository(AppDbContext dbContext)
    : IProfessionalAvailabilityRepository
{
    public Task<bool> ProfessionalExistsAsync(
        Guid professionalId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Professionals.AsNoTracking().AnyAsync(
            professional => professional.Id == professionalId && professional.CompanyId == companyId,
            cancellationToken);

    public Task<ProfessionalAvailability?> GetAvailabilityAsync(
        Guid professionalId,
        Guid companyId,
        bool trackChanges,
        CancellationToken cancellationToken = default)
    {
        var query = trackChanges
            ? dbContext.ProfessionalAvailabilities
            : dbContext.ProfessionalAvailabilities.AsNoTracking();

        return query.Include(availability => availability.DailySchedules).SingleOrDefaultAsync(
            availability => availability.ProfessionalId == professionalId && availability.CompanyId == companyId,
            cancellationToken);
    }

    public async Task<IReadOnlyList<ScheduleBlock>> GetScheduleBlocksAsync(
        Guid professionalId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleBlocks.AsNoTracking()
            .Where(block => block.ProfessionalId == professionalId && block.CompanyId == companyId)
            .OrderBy(block => block.StartDate)
            .ThenBy(block => block.StartTime)
            .ThenBy(block => block.CreatedAtUtc)
            .ToArrayAsync(cancellationToken);

    public Task<ScheduleBlock?> GetScheduleBlockAsync(
        Guid scheduleBlockId,
        Guid professionalId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.ScheduleBlocks.SingleOrDefaultAsync(
            block =>
                block.Id == scheduleBlockId &&
                block.ProfessionalId == professionalId &&
                block.CompanyId == companyId,
            cancellationToken);

    public async Task AddAvailabilityAsync(
        ProfessionalAvailability availability,
        CancellationToken cancellationToken = default) =>
        await dbContext.ProfessionalAvailabilities.AddAsync(availability, cancellationToken);

    public async Task AddScheduleBlockAsync(
        ScheduleBlock scheduleBlock,
        CancellationToken cancellationToken = default) =>
        await dbContext.ScheduleBlocks.AddAsync(scheduleBlock, cancellationToken);

    public void RemoveScheduleBlock(ScheduleBlock scheduleBlock) =>
        dbContext.ScheduleBlocks.Remove(scheduleBlock);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) =>
        dbContext.SaveChangesAsync(cancellationToken);
}
