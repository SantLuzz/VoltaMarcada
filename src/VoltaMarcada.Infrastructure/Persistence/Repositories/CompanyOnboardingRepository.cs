using Microsoft.EntityFrameworkCore;
using Npgsql;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Domain.Entities;

namespace VoltaMarcada.Infrastructure.Persistence.Repositories;

public sealed class CompanyOnboardingRepository(AppDbContext dbContext) : ICompanyOnboardingRepository
{
    private const string DuplicateMembershipConstraintName = "UX_CompanyUsers_UserId";
    private const string DuplicateDocumentConstraintName = "UX_Companies_Document";
    private const string DuplicateSlugConstraintName = "UX_Companies_Slug";

    public Task<bool> HasMembershipAsync(Guid userId, CancellationToken cancellationToken = default) =>
        dbContext.CompanyUsers.AnyAsync(companyUser => companyUser.UserId == userId, cancellationToken);

    public Task<bool> DocumentExistsAsync(string normalizedDocument, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AnyAsync(company => company.Document == normalizedDocument, cancellationToken);

    public Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default) =>
        dbContext.Companies.AnyAsync(company => company.Slug == slug, cancellationToken);

    public Task<bool> IsCityInStateAsync(Guid cityId, Guid stateId, CancellationToken cancellationToken = default) =>
        dbContext.Cities.AnyAsync(city => city.Id == cityId && city.StateId == stateId, cancellationToken);

    public async Task<CompanyOnboardingPersistenceOutcome> CreateCompanyAndOwnerAsync(
        Company company,
        CompanyUser ownerMembership,
        CancellationToken cancellationToken = default)
    {
        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            await dbContext.Companies.AddAsync(company, cancellationToken);
            await dbContext.CompanyUsers.AddAsync(ownerMembership, cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            return CompanyOnboardingPersistenceOutcome.Success;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException postgresException)
        {
            await transaction.RollbackAsync(cancellationToken);

            if (postgresException.ConstraintName == DuplicateDocumentConstraintName)
            {
                return CompanyOnboardingPersistenceOutcome.DuplicateDocument;
            }

            if (postgresException.ConstraintName == DuplicateMembershipConstraintName)
            {
                return CompanyOnboardingPersistenceOutcome.DuplicateMembership;
            }

            if (postgresException.ConstraintName == DuplicateSlugConstraintName)
            {
                return CompanyOnboardingPersistenceOutcome.DuplicateSlug;
            }

            throw;
        }
    }
}
