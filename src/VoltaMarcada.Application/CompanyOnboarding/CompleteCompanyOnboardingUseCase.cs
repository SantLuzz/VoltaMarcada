using FluentValidation;
using VoltaMarcada.Application.Abstractions.Identity;
using VoltaMarcada.Application.Abstractions.Operations;
using VoltaMarcada.Application.Abstractions.Persistence;
using VoltaMarcada.Application.Abstractions.Storage;
using VoltaMarcada.Domain.Entities;
using VoltaMarcada.Domain.Enums;
using VoltaMarcada.Domain.ValueObjects;

namespace VoltaMarcada.Application.CompanyOnboarding;

public sealed class CompleteCompanyOnboardingUseCase(
    ICurrentUser currentUser,
    ICompanyOnboardingRepository repository,
    IFileStorage fileStorage,
    IValidator<CompleteCompanyOnboardingCommand> validator,
    ICompanyLicenseTrialConfiguration licenseTrialConfiguration,
    TimeProvider timeProvider)
{
    public async Task<OperationResult<Guid>> ExecuteAsync(
        CompleteCompanyOnboardingCommand command,
        CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || currentUser.UserId == Guid.Empty)
        {
            return OperationResult<Guid>.Failure(CompanyOnboardingErrors.Unauthenticated);
        }

        var validationResult = await validator.ValidateAsync(command, cancellationToken);
        if (!validationResult.IsValid || !BrazilianDocument.TryNormalize(command.Document, out var normalizedDocument))
        {
            return OperationResult<Guid>.Failure(CompanyOnboardingErrors.InvalidInput);
        }

        if (await repository.HasMembershipAsync(currentUser.UserId, cancellationToken))
        {
            return OperationResult<Guid>.Failure(CompanyOnboardingErrors.AlreadyOnboarded);
        }

        if (await repository.DocumentExistsAsync(normalizedDocument, cancellationToken))
        {
            return OperationResult<Guid>.Failure(CompanyOnboardingErrors.DocumentAlreadyExists);
        }

        if (!await repository.IsCityInStateAsync(command.CityId, command.StateId, cancellationToken))
        {
            return OperationResult<Guid>.Failure(CompanyOnboardingErrors.InvalidLocation);
        }

        var baseSlug = CompanySlugGenerator.GenerateBaseSlug(command.TradeName);
        if (string.IsNullOrEmpty(baseSlug))
        {
            return OperationResult<Guid>.Failure(CompanyOnboardingErrors.InvalidInput);
        }

        var slug = await repository.SlugExistsAsync(baseSlug, cancellationToken)
            ? $"{baseSlug}-{normalizedDocument[^4..]}"
            : baseSlug;

        string? logoKey = null;
        if (command.Logo is not null)
        {
            if (!IsValidLogo(command.Logo))
            {
                return OperationResult<Guid>.Failure(CompanyOnboardingErrors.InvalidLogo);
            }

            var upload = await fileStorage.UploadAsync(
                new FileStorageUploadRequest(command.Logo, "company-logos"),
                cancellationToken);
            logoKey = upload.Key;
        }

        var now = timeProvider.GetUtcNow();
        var company = Company.Create(
            command.LegalName,
            command.TradeName,
            normalizedDocument,
            command.WhatsApp,
            command.PostalCode,
            command.Address,
            command.StateId,
            command.CityId,
            logoKey,
            slug,
            now);
        var ownerMembership = CompanyUser.Create(currentUser.UserId, company.Id, CompanyUserRole.Owner, now);

        var license = CompanyLicense.Create(company.Id, now, licenseTrialConfiguration.TrialDays);
        var outcome = await repository.CreateCompanyAndOwnerAsync(company, ownerMembership, license, cancellationToken);
        if (outcome != CompanyOnboardingPersistenceOutcome.Success && logoKey is not null)
        {
            await fileStorage.DeleteAsync(logoKey, cancellationToken);
        }
        return outcome switch
        {
            CompanyOnboardingPersistenceOutcome.Success => OperationResult<Guid>.Success(company.Id),
            CompanyOnboardingPersistenceOutcome.DuplicateMembership => OperationResult<Guid>.Failure(CompanyOnboardingErrors.AlreadyOnboarded),
            CompanyOnboardingPersistenceOutcome.DuplicateDocument => OperationResult<Guid>.Failure(CompanyOnboardingErrors.DocumentAlreadyExists),
            CompanyOnboardingPersistenceOutcome.DuplicateSlug => OperationResult<Guid>.Failure(CompanyOnboardingErrors.SlugAlreadyExists),
            _ => throw new InvalidOperationException($"Unsupported company onboarding persistence outcome: {outcome}.")
        };
    }

    private static bool IsValidLogo(FileUpload logo) =>
        logo.Length is > 0 and <= 5 * 1024 * 1024 &&
        logo.ContentType is "image/png" or "image/jpeg" or "image/webp";
}
