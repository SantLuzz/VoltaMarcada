using FluentValidation;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Application.Abstractions.Storage;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.CompanyOnboarding;

public sealed class CompleteCompanyOnboardingUseCase(
    ICurrentUser currentUser,
    ICompanyOnboardingRepository repository,
    ICompanyLogoStorage logoStorage,
    IValidator<CompleteCompanyOnboardingCommand> validator,
    TimeProvider timeProvider)
{
    public async Task<CompleteCompanyOnboardingResult> ExecuteAsync(
        CompleteCompanyOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return CompleteCompanyOnboardingResult.Unauthenticated();
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var normalizedDocument))
        {
            return CompleteCompanyOnboardingResult.InvalidInput();
        }

        if (await repository.HasMembershipAsync(currentUser.UserId, cancellationToken))
        {
            return CompleteCompanyOnboardingResult.AlreadyOnboarded();
        }

        if (await repository.DocumentExistsAsync(normalizedDocument, cancellationToken))
        {
            return CompleteCompanyOnboardingResult.DocumentAlreadyExists();
        }

        if (!await repository.IsCityInStateAsync(command.CityId, command.StateId, cancellationToken))
        {
            return new CompleteCompanyOnboardingResult(CompleteCompanyOnboardingResultCode.InvalidLocation, null);
        }

        var baseSlug = CompanySlugGenerator.GenerateBaseSlug(command.TradeName);
        if (string.IsNullOrEmpty(baseSlug))
        {
            return CompleteCompanyOnboardingResult.InvalidInput();
        }

        var slug = await repository.SlugExistsAsync(baseSlug, cancellationToken)
            ? $"{baseSlug}-{normalizedDocument[^4..]}"
            : baseSlug;

        string? logoKey = null;
        if (command.Logo is not null)
        {
            var logoResult = await logoStorage.SaveAsync(command.Logo, cancellationToken);
            if (logoResult.Code != CompanyLogoStorageResultCode.Success || logoResult.LogoKey is null)
            {
                return new CompleteCompanyOnboardingResult(CompleteCompanyOnboardingResultCode.InvalidLogo, null);
            }

            logoKey = logoResult.LogoKey;
        }

        var now = timeProvider.GetUtcNow();
        var company = Company.Create(
            command.LegalName,
            command.TradeName,
            normalizedDocument,
            command.WhatsApp,
            command.PostalCode,
            command.StateId,
            command.CityId,
            logoKey,
            slug,
            now);
        var ownerMembership = CompanyUser.Create(currentUser.UserId, company.Id, CompanyUserRole.Owner, now);

        var outcome = await repository.CreateCompanyAndOwnerAsync(company, ownerMembership, cancellationToken);
        if (outcome != CompanyOnboardingPersistenceOutcome.Success && logoKey is not null)
        {
            await logoStorage.DeleteAsync(logoKey, cancellationToken);
        }
        return outcome switch
        {
            CompanyOnboardingPersistenceOutcome.Success => CompleteCompanyOnboardingResult.Success(company.Id),
            CompanyOnboardingPersistenceOutcome.DuplicateMembership => CompleteCompanyOnboardingResult.AlreadyOnboarded(),
            CompanyOnboardingPersistenceOutcome.DuplicateDocument => CompleteCompanyOnboardingResult.DocumentAlreadyExists(),
            CompanyOnboardingPersistenceOutcome.DuplicateSlug => CompleteCompanyOnboardingResult.SlugAlreadyExists(),
            _ => throw new InvalidOperationException($"Unsupported company onboarding persistence outcome: {outcome}.")
        };
    }
}
