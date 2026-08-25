using Microsoft.EntityFrameworkCore;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Professionals;
using VoltaMarcada.Infrastructure.Identity;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class ProfessionalRepository(AppDbContext dbContext) : IProfessionalRepository
{
    public async Task AddAsync(global::VoltaMarcada.Domain.Entities.Professional professional, CancellationToken cancellationToken = default) =>
        await dbContext.Professionals.AddAsync(professional, cancellationToken);

    public async Task<IReadOnlyList<ProfessionalListItem>> GetAllForCompanyAsync(
        Guid companyId,
        string? searchFilter,
        CancellationToken cancellationToken = default)
    {
        var query = dbContext.Professionals.AsNoTracking()
            .Where(professional => professional.CompanyId == companyId)
            .Join(
                dbContext.CompanyUsers.AsNoTracking().Where(member => member.CompanyId == companyId),
                professional => professional.CompanyUserId,
                member => member.Id,
                (professional, member) => new { Professional = professional, Member = member })
            .Join(
                dbContext.Users.AsNoTracking(),
                item => item.Member.UserId,
                user => user.Id,
                (item, user) => new { item.Professional, item.Member, User = user });
        if (!string.IsNullOrWhiteSpace(searchFilter))
        {
            var pattern = $"%{searchFilter.Trim().Replace("\\", "\\\\").Replace("%", "\\%").Replace("_", "\\_")}%";
            query = query.Where(item =>
                EF.Functions.ILike(item.User.FullName ?? item.User.Email ?? string.Empty, pattern, "\\") ||
                EF.Functions.ILike(item.User.Email ?? string.Empty, pattern, "\\"));
        }

        var results = await query
            .OrderBy(item => item.User.FullName ?? item.User.Email ?? string.Empty)
            .ToListAsync(cancellationToken);

        var professionalIds = results.Select(item => item.Professional.Id).ToArray();
        var services = await dbContext.ProfessionalServices.AsNoTracking()
            .Where(link => link.CompanyId == companyId && professionalIds.Contains(link.ProfessionalId))
            .Join(
                dbContext.Services.AsNoTracking().Where(service => service.CompanyId == companyId),
                link => link.ServiceId,
                service => service.Id,
                (link, service) => new { link.ProfessionalId, service.Id, service.Name })
            .ToListAsync(cancellationToken);

        return results.Select(item => new ProfessionalListItem(
            item.Professional,
            item.User.FullName ?? item.User.Email ?? string.Empty,
            item.User.Email ?? string.Empty,
            services.Where(service => service.ProfessionalId == item.Professional.Id)
                .Select(service => service.Id)
                .ToArray(),
            services.Where(service => service.ProfessionalId == item.Professional.Id)
                .Select(service => service.Name)
                .OrderBy(name => name)
                .ToArray()))
            .ToArray();
    }

    public Task<global::VoltaMarcada.Domain.Entities.Professional?> GetForCompanyAsync(
        Guid professionalId,
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        dbContext.Professionals
            .Include(professional => professional.ProfessionalServices)
            .SingleOrDefaultAsync(professional => professional.Id == professionalId && professional.CompanyId == companyId, cancellationToken);

    public async Task<ProfessionalListItem?> GetForCurrentUserAsync(
        Guid userId,
        Guid companyId,
        CancellationToken cancellationToken = default)
    {
        var item = await dbContext.Professionals.AsNoTracking()
            .Where(professional => professional.CompanyId == companyId)
            .Join(
                dbContext.CompanyUsers.AsNoTracking().Where(member => member.CompanyId == companyId && member.UserId == userId),
                professional => professional.CompanyUserId,
                member => member.Id,
                (professional, member) => new { Professional = professional, Member = member })
            .Join(
                dbContext.Users.AsNoTracking(),
                entry => entry.Member.UserId,
                user => user.Id,
                (entry, user) => new
                {
                    entry.Professional,
                    entry.Member.UserId,
                    Name = user.FullName ?? user.Email ?? string.Empty,
                    Email = user.Email ?? string.Empty
                })
            .SingleOrDefaultAsync(cancellationToken);
        if (item is null)
        {
            return null;
        }

        var serviceNames = await dbContext.ProfessionalServices.AsNoTracking()
            .Where(link => link.ProfessionalId == item.Professional.Id && link.CompanyId == companyId)
            .Join(dbContext.Services.AsNoTracking().Where(service => service.CompanyId == companyId), link => link.ServiceId, service => service.Id, (_, service) => service.Name)
            .OrderBy(name => name)
            .ToArrayAsync(cancellationToken);

        var serviceIds = await dbContext.ProfessionalServices.AsNoTracking()
            .Where(link => link.ProfessionalId == item.Professional.Id && link.CompanyId == companyId)
            .Select(link => link.ServiceId)
            .ToArrayAsync(cancellationToken);

        return new ProfessionalListItem(item.Professional, item.Name, item.Email, serviceIds, serviceNames);
    }

    public async Task<IReadOnlyList<CompanyMemberOption>> GetAvailableCompanyMembersAsync(
        Guid companyId,
        Guid? professionalId = null,
        CancellationToken cancellationToken = default)
    {
        var linkedMemberIds = dbContext.Professionals.AsNoTracking()
            .Where(professional => professional.CompanyId == companyId && (!professionalId.HasValue || professional.Id != professionalId.Value))
            .Select(professional => professional.CompanyUserId);

        return await dbContext.CompanyUsers.AsNoTracking()
            .Where(member => member.CompanyId == companyId && !linkedMemberIds.Contains(member.Id))
            .Join(
                dbContext.Users.AsNoTracking(),
                member => member.UserId,
                user => user.Id,
                (member, user) => new { Member = member, User = user })
            .OrderBy(item => item.User.FullName ?? item.User.Email ?? string.Empty)
            .Select(item => new CompanyMemberOption(
                item.Member.Id,
                item.User.FullName ?? item.User.Email ?? string.Empty,
                item.User.Email ?? string.Empty))
            .ToArrayAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<ProfessionalServiceOption>> GetActiveServicesAsync(
        Guid companyId,
        CancellationToken cancellationToken = default) =>
        await dbContext.Services.AsNoTracking()
            .Where(service => service.CompanyId == companyId && service.IsActive)
            .OrderBy(service => service.Name)
            .Select(service => new ProfessionalServiceOption(service.Id, service.Name))
            .ToArrayAsync(cancellationToken);

    public Task<bool> IsCompanyMemberAvailableAsync(
        Guid companyUserId,
        Guid companyId,
        Guid? excludedProfessionalId = null,
        CancellationToken cancellationToken = default) =>
        dbContext.CompanyUsers.AsNoTracking().AnyAsync(member =>
            member.Id == companyUserId &&
            member.CompanyId == companyId &&
            !dbContext.Professionals.Any(professional =>
                professional.CompanyId == companyId &&
                professional.CompanyUserId == companyUserId &&
                (!excludedProfessionalId.HasValue || professional.Id != excludedProfessionalId.Value)),
            cancellationToken);

    public async Task<bool> AreActiveServicesValidAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> serviceIds,
        CancellationToken cancellationToken = default) =>
        serviceIds.Count > 0 &&
        serviceIds.All(serviceId => serviceId != Guid.Empty) &&
        await dbContext.Services.AsNoTracking()
            .CountAsync(service => service.CompanyId == companyId && service.IsActive && serviceIds.Contains(service.Id), cancellationToken) == serviceIds.Distinct().Count();

    public async Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> professionalIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Professionals.AsNoTracking()
            .Where(professional => professional.CompanyId == companyId && professionalIds.Contains(professional.Id))
            .ToDictionaryAsync(professional => professional.Id, professional => professional.TimeZoneId, cancellationToken);

    public async Task<IReadOnlyDictionary<Guid, string>> GetDisplayNamesAsync(
        Guid companyId,
        IReadOnlyCollection<Guid> professionalIds,
        CancellationToken cancellationToken = default) =>
        await dbContext.Professionals.AsNoTracking()
            .Where(professional => professional.CompanyId == companyId && professionalIds.Contains(professional.Id))
            .Join(
                dbContext.CompanyUsers.AsNoTracking().Where(member => member.CompanyId == companyId),
                professional => professional.CompanyUserId,
                member => member.Id,
                (professional, member) => new { professional.Id, member.UserId })
            .Join(
                dbContext.Users.AsNoTracking(),
                item => item.UserId,
                user => user.Id,
                (item, user) => new { item.Id, Name = user.FullName ?? user.Email ?? string.Empty })
            .ToDictionaryAsync(item => item.Id, item => item.Name, cancellationToken);

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => dbContext.SaveChangesAsync(cancellationToken);

}
