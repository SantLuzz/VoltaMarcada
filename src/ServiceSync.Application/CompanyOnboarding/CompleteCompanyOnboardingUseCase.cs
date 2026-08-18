using FluentValidation;
using ServiceSync.Application.Abstractions.Identity;
using ServiceSync.Application.Abstractions.Persistence;
using ServiceSync.Domain.Entities;
using ServiceSync.Domain.Enums;
using ServiceSync.Domain.ValueObjects;

namespace ServiceSync.Application.CompanyOnboarding;

public sealed class CompleteCompanyOnboardingUseCase(
    ICurrentUser currentUser,
    ICompanyOnboardingRepository repository,
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

        var baseSlug = CompanySlugGenerator.GenerateBaseSlug(command.TradeName);
        if (string.IsNullOrEmpty(baseSlug))
        {
            return CompleteCompanyOnboardingResult.InvalidInput();
        }

        var slug = await repository.SlugExistsAsync(baseSlug, cancellationToken)
            ? $"{baseSlug}-{normalizedDocument[^4..]}"
            : baseSlug;

        var now = timeProvider.GetUtcNow();
        var company = Company.Create(
            command.LegalName,
            command.TradeName,
            normalizedDocument,
            command.Phone,
            command.WhatsApp,
            command.City,
            command.State,
            command.LogoKey,
            slug,
            now);
        var ownerMembership = CompanyUser.Create(currentUser.UserId, company.Id, CompanyUserRole.Owner, now);

        var outcome = await repository.CreateCompanyAndOwnerAsync(company, ownerMembership, cancellationToken);
        return outcome switch
        {
            CompanyOnboardingPersistenceOutcome.Success => CompleteCompanyOnboardingResult.Success(company.Id),
            CompanyOnboardingPersistenceOutcome.DuplicateDocument => CompleteCompanyOnboardingResult.DocumentAlreadyExists(),
            CompanyOnboardingPersistenceOutcome.DuplicateSlug => CompleteCompanyOnboardingResult.SlugAlreadyExists(),
            _ => throw new InvalidOperationException($"Unsupported company onboarding persistence outcome: {outcome}.")
        };
    }
}
