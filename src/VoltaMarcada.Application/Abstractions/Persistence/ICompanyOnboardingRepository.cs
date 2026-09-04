using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Application.Abstractions.Persistence;

public interface ICompanyOnboardingRepository
{
    Task<bool> HasMembershipAsync(Guid userId, CancellationToken cancellationToken = default);

    Task<bool> DocumentExistsAsync(string normalizedDocument, CancellationToken cancellationToken = default);

    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);

    Task<bool> IsCityInStateAsync(Guid cityId, Guid stateId, CancellationToken cancellationToken = default);

    Task<CompanyOnboardingPersistenceOutcome> CreateCompanyAndOwnerAsync(
        Company company,
        CompanyUser ownerMembership,
        CompanyLicense license,
        CancellationToken cancellationToken = default);
}

public enum CompanyOnboardingPersistenceOutcome
{
    Success,
    DuplicateMembership,
    DuplicateDocument,
    DuplicateSlug
}
