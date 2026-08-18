### Task 2: Add onboarding application behavior

**Files:**
- Create: `src/ServiceSync.Application/Abstractions/Identity/ICurrentUser.cs`
- Create: `src/ServiceSync.Application/Abstractions/Tenancy/ICurrentCompany.cs`
- Create: `src/ServiceSync.Application/Abstractions/Persistence/ICompanyOnboardingRepository.cs`
- Create: `src/ServiceSync.Application/CompanyOnboarding/CompleteCompanyOnboardingCommand.cs`
- Create: `src/ServiceSync.Application/CompanyOnboarding/CompleteCompanyOnboardingResult.cs`
- Create: `src/ServiceSync.Application/CompanyOnboarding/CompleteCompanyOnboardingCommandValidator.cs`
- Create: `src/ServiceSync.Application/CompanyOnboarding/CompanySlugGenerator.cs`
- Create: `src/ServiceSync.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`

**Consumes:** Task 1 domain APIs and `TimeProvider`.

**Produces:**

```csharp
public interface ICurrentUser { bool IsAuthenticated { get; } Guid UserId { get; } }
public interface ICurrentCompany { Guid CompanyId { get; } }
public sealed record CompleteCompanyOnboardingCommand(string LegalName, string TradeName,
    string Document, string Phone, string WhatsApp, string City, string State, string? LogoKey);
public Task<CompleteCompanyOnboardingResult> ExecuteAsync(
    CompleteCompanyOnboardingCommand command, CancellationToken cancellationToken);
```

- [ ] Define repository methods to check membership, check document/slug existence, and atomically persist `Company` plus `CompanyUser`. Its persistence outcome enum must have `Success`, `DuplicateDocument`, and `DuplicateSlug`.
- [ ] Add the command and FluentValidation validator. Require every non-logo field; invoke `BrazilianDocument.TryNormalize` for the document; return clear result codes including `Unauthenticated`, `AlreadyOnboarded`, `InvalidInput`, `DocumentAlreadyExists`, `SlugAlreadyExists`, and `Success`.
- [ ] Implement slug normalization: lowercase ASCII, remove diacritics, replace non-alphanumeric runs with one hyphen, trim hyphens; use `baseSlug` when available and `$"{baseSlug}-{normalizedDocument[^4..]}"` after collision.
- [ ] Implement the use case: obtain `ICurrentUser`, validate input, normalize document, reject an existing membership/document, create domain objects with `timeProvider.GetUtcNow()`, choose a free slug, and translate transaction results to the explicit result type. Never accept a user ID or company ID in the command.
- [ ] Register `TimeProvider.System` and the use case in application DI, then run `dotnet build ServiceSync.sln`; expect exit code 0.
