using ServiceSync.Application.Professionals;
using ServiceSync.Domain.Entities;

namespace ServiceSync.Application.Abstractions.Persistence;

public interface IProfessionalRepository
{
    Task AddAsync(Professional professional, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfessionalListItem>> GetAllForCompanyAsync(Guid companyId, string? searchFilter, CancellationToken cancellationToken = default);

    Task<Professional?> GetForCompanyAsync(Guid professionalId, Guid companyId, CancellationToken cancellationToken = default);

    Task<ProfessionalListItem?> GetForCurrentUserAsync(Guid userId, Guid companyId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyMemberOption>> GetAvailableCompanyMembersAsync(Guid companyId, Guid? professionalId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProfessionalServiceOption>> GetActiveServicesAsync(Guid companyId, CancellationToken cancellationToken = default);

    Task<bool> IsCompanyMemberAvailableAsync(Guid companyUserId, Guid companyId, Guid? excludedProfessionalId = null, CancellationToken cancellationToken = default);

    Task<bool> AreActiveServicesValidAsync(Guid companyId, IReadOnlyCollection<Guid> serviceIds, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<Guid, string>> GetTimeZoneIdsAsync(Guid companyId, IReadOnlyCollection<Guid> professionalIds, CancellationToken cancellationToken = default);

    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public sealed record ProfessionalListItem(
    Professional Professional,
    string CompanyMemberName,
    string CompanyMemberEmail,
    IReadOnlyList<Guid> ServiceIds,
    IReadOnlyList<string> ServiceNames);

public sealed record CompanyMemberOption(Guid Id, string Name, string Email);

public sealed record ProfessionalServiceOption(Guid Id, string Name);
