# Task 2 Report: Company Onboarding Application Behavior

## Changed Files

- `src/VoltaMarcada.Application/Abstractions/Identity/ICurrentUser.cs`
- `src/VoltaMarcada.Application/Abstractions/Tenancy/ICurrentCompany.cs`
- `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyOnboardingRepository.cs`
- `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommand.cs`
- `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingResult.cs`
- `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommandValidator.cs`
- `src/VoltaMarcada.Application/CompanyOnboarding/CompanySlugGenerator.cs`
- `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`
- `src/VoltaMarcada.Application/DependencyInjection.cs`
- `.superpowers/sdd/2026-08-18-company-onboarding/task-2-report.md`

## Exact Public Types and Signatures

```csharp
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    Guid UserId { get; }
}

public interface ICurrentCompany
{
    Guid CompanyId { get; }
}

public interface ICompanyOnboardingRepository
{
    Task<bool> HasMembershipAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<bool> DocumentExistsAsync(string normalizedDocument, CancellationToken cancellationToken = default);
    Task<bool> SlugExistsAsync(string slug, CancellationToken cancellationToken = default);
    Task<CompanyOnboardingPersistenceOutcome> CreateCompanyAndOwnerAsync(
        Company company,
        CompanyUser ownerMembership,
        CancellationToken cancellationToken = default);
}

public enum CompanyOnboardingPersistenceOutcome
{
    Success,
    DuplicateDocument,
    DuplicateSlug
}

public sealed record CompleteCompanyOnboardingCommand(
    string LegalName,
    string TradeName,
    string Document,
    string Phone,
    string WhatsApp,
    string City,
    string State,
    string? LogoKey);

public enum CompleteCompanyOnboardingResultCode
{
    Unauthenticated,
    AlreadyOnboarded,
    InvalidInput,
    DocumentAlreadyExists,
    SlugAlreadyExists,
    Success
}

public sealed record CompleteCompanyOnboardingResult(
    CompleteCompanyOnboardingResultCode Code,
    Guid? CompanyId);

public static class CompanySlugGenerator
{
    public static string GenerateBaseSlug(string? value);
}

public sealed class CompleteCompanyOnboardingUseCase
{
    public Task<CompleteCompanyOnboardingResult> ExecuteAsync(
        CompleteCompanyOnboardingCommand command,
        CancellationToken cancellationToken);
}
```

`CompleteCompanyOnboardingResult` also exposes static factory methods for every result code. `CompleteCompanyOnboardingCommandValidator` is public and registered through the existing assembly scan.

## Implementation Summary

- The use case resolves the authenticated user only from `ICurrentUser`; the command contains neither user nor company identifiers.
- It validates required input with FluentValidation, normalizes the Brazilian document, rejects existing memberships and documents, generates a deterministic slug from the trade name, and creates `Company` plus an owner `CompanyUser` using `TimeProvider.GetUtcNow()`.
- The onboarding repository contract provides the membership/document/slug checks and a single atomic persistence operation with explicit duplicate outcomes.
- The slug generator removes diacritics, emits lowercase ASCII alphanumeric characters, coalesces separators as hyphens, and trims leading/trailing hyphens. A base-slug collision uses the normalized document's final four digits.
- Application DI now registers `TimeProvider.System` and `CompleteCompanyOnboardingUseCase`.

## Build Command and Output Summary

Command run:

```text
dotnet build VoltaMarcada.sln
```

Result: exit code 1. `VoltaMarcada.Domain` and `VoltaMarcada.Application` compiled successfully. The solution build is blocked solely by the known out-of-scope Infrastructure error in `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`: the configuration references the removed `Company.Name` property (CS1061). Restore also emitted NU1900 warnings because package vulnerability feeds were unreachable.

## Concerns

- The existing Infrastructure `CompanyConfiguration` must be updated by its owning scoped task; it was intentionally not changed here.
- `ICompanyOnboardingRepository`, `ICurrentUser`, and `ICurrentCompany` require Infrastructure/Web implementations and composition registrations in their owning follow-up tasks.
- No automated tests were added or run, following the explicit user decision.
