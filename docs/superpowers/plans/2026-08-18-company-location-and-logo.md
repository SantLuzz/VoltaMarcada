# Company Location and Logo Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add state/city reference data, CEP, WhatsApp-only company contact, and local GUID-based company-logo upload to the onboarding flow.

**Architecture:** `State` and `City` are shared reference entities identified by GUIDs. Application use cases validate the selected state/city relationship and coordinate logo persistence through an application storage abstraction; Infrastructure persists the reference data, company relations, and local files. The Web layer renders dependent selects and posts a multipart form without accepting tenant identifiers.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** `docs/superpowers/specs/2026-08-18-company-location-and-logo-design.md`

## Global Constraints

- All source identifiers, migration names, logs, and technical messages are English; UI copy is Brazilian Portuguese.
- `State` has `Id`, `Name`, and `Abbreviation`; `City` has `Id`, `StateId`, and `Name`; all IDs are `Guid`.
- `Company` stores `StateId`, `CityId`, `PostalCode`, required `WhatsApp`, and optional `LogoKey`; it no longer accepts `Phone`, free-text `State`, or free-text `City`.
- Seed all 26 states, the Federal District, and every municipality in a deterministic migration with no runtime network dependency.
- Upload only PNG, JPEG, and WebP logos up to 5 MB. Generated file names use GUIDs and `LogoKey` stores a relative path.
- Preserve and validate local `returnUrl` values on every onboarding redirect.
- Do not run `dotnet build`, `dotnet test`, the application, migrations, or database commands unless the user explicitly requests execution.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| `src/VoltaMarcada.Domain/Entities/State.cs` | State reference entity invariants. |
| `src/VoltaMarcada.Domain/Entities/City.cs` | City reference entity and state relationship invariant. |
| `src/VoltaMarcada.Domain/ValueObjects/BrazilianPostalCode.cs` | CEP normalization and validation. |
| `src/VoltaMarcada.Domain/Entities/Company.cs` | Company location, CEP, WhatsApp, and logo state. |
| `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyOnboardingRepository.cs` | State/city ownership validation during company onboarding. |
| `src/VoltaMarcada.Application/Abstractions/Storage/ICompanyLogoStorage.cs` | Application-level logo storage contract. |
| `src/VoltaMarcada.Infrastructure/Storage/CompanyLogoStorageOptions.cs` | Configurable local logo root path. |
| `src/VoltaMarcada.Application/CompanyOnboarding/*` | New onboarding command, validation, outcomes, and use case behavior. |
| `src/VoltaMarcada.Application/CompanyLocations/*` | Explicit state and city read use cases for the onboarding page. |
| `src/VoltaMarcada.Infrastructure/Persistence/Configurations/{State,City,Company}Configuration.cs` | EF mappings, foreign keys, and indexes. |
| `src/VoltaMarcada.Infrastructure/Persistence/LocationSeedData.cs` | Generated, versioned catalog with fixed GUIDs. |
| `src/VoltaMarcada.Infrastructure/Persistence/Repositories/*` | Location reads and company onboarding ownership checks. |
| `src/VoltaMarcada.Infrastructure/Storage/LocalCompanyLogoStorage.cs` | Temporary local-file implementation. |
| `src/VoltaMarcada.Infrastructure/Persistence/Migrations/*AddCompanyLocationsAndLogo.cs` | Schema and catalog data transition. |
| `src/VoltaMarcada.Web/Endpoints/CompanyOnboardingEndpoints.cs` | Multipart binding, localized failures, and no tenant IDs. |
| `src/VoltaMarcada.Web/Components/Pages/CompanyOnboarding.razor` | Logo input, CEP, and dependent state/city selects. |
| `tests/VoltaMarcada.Domain.Tests/*` | Pure CEP and entity invariant tests. |
| `tests/VoltaMarcada.Application.Tests/*` | State/city and logo use case tests. |
| `tests/VoltaMarcada.Domain.Tests/VoltaMarcada.Domain.Tests.csproj` | Domain test project. |
| `tests/VoltaMarcada.Application.Tests/VoltaMarcada.Application.Tests.csproj` | Application test project. |

### Task 1: Create the location and postal-code domain model

**Files:**

- Create: `src/VoltaMarcada.Domain/Entities/State.cs`
- Create: `src/VoltaMarcada.Domain/Entities/City.cs`
- Create: `src/VoltaMarcada.Domain/ValueObjects/BrazilianPostalCode.cs`
- Modify: `src/VoltaMarcada.Domain/Entities/Company.cs`
- Create: `tests/VoltaMarcada.Domain.Tests/VoltaMarcada.Domain.Tests.csproj`
- Create: `tests/VoltaMarcada.Domain.Tests/BrazilianPostalCodeTests.cs`

**Consumes:** Existing `DomainException` and `BrazilianDocument` conventions.

**Produces:**

```csharp
public sealed class State
{
    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public string Abbreviation { get; private set; }
}

public sealed class City
{
    public Guid Id { get; private set; }
    public Guid StateId { get; private set; }
    public string Name { get; private set; }
}

public static bool TryNormalize(string? value, out string normalizedPostalCode);
```

- [ ] **Step 1: Add failing CEP tests**

Create the domain test project with references to `VoltaMarcada.Domain`, `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`, then add the test class.

```csharp
[Theory]
[InlineData("79002-123", "79002123")]
[InlineData("79002123", "79002123")]
public void TryNormalize_ShouldReturnEightDigits_WhenPostalCodeIsValid(
    string postalCode, string expected)
{
    var result = BrazilianPostalCode.TryNormalize(postalCode, out var normalized);

    Assert.True(result);
    Assert.Equal(expected, normalized);
}

[Fact]
public void TryNormalize_ShouldReturnFalse_WhenPostalCodeDoesNotContainEightDigits()
{
    var result = BrazilianPostalCode.TryNormalize("79002-12", out _);

    Assert.False(result);
}
```

- [ ] **Step 2: Implement `BrazilianPostalCode.TryNormalize`**

Retain decimal digits only and return `true` only for exactly eight digits. Do not call external services.

- [ ] **Step 3: Implement `State` and `City` factories**

Use private EF constructors and `Create(Guid id, string name, string abbreviation)` / `Create(Guid id, Guid stateId, string name)` factories. Reject empty GUIDs, empty text, and state abbreviations other than two uppercase characters.

- [ ] **Step 4: Replace company phone and free-text location state**

Change `Company.Create` to accept `string whatsApp, string postalCode, Guid stateId, Guid cityId, string? logoKey, string slug, DateTimeOffset now`. Normalize CEP through `BrazilianPostalCode`, reject empty location IDs, remove the `Phone`, `City`, and `State` domain properties, and add `StateId`, `CityId`, and `PostalCode`.

- [ ] **Step 5: Add company invariant tests**

Add tests that assert `Company.Create` rejects an empty `stateId`, empty `cityId`, and invalid CEP.

### Task 2: Generate and version the complete Brazilian location catalog

**Files:**

- Create: `scripts/GenerateBrazilianLocationSeed.ps1`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/LocationSeedData.cs`
- Create: `tests/VoltaMarcada.Application.Tests/VoltaMarcada.Application.Tests.csproj`
- Create: `tests/VoltaMarcada.Application.Tests/LocationSeedDataTests.cs`

**Consumes:** IBGE state endpoint `https://servicodados.ibge.gov.br/api/v1/localidades/estados` and per-state municipality endpoint `https://servicodados.ibge.gov.br/api/v1/localidades/estados/{stateCode}/municipios` during generation only.

**Produces:**

```csharp
public static class LocationSeedData
{
    public static IReadOnlyList<StateSeed> States { get; }
    public static IReadOnlyList<CitySeed> Cities { get; }
}

public sealed record StateSeed(Guid Id, string Name, string Abbreviation);
public sealed record CitySeed(Guid Id, Guid StateId, string Name);
```

- [ ] **Step 1: Create a deterministic GUID generator in the PowerShell script**

Create the application test project with references to `VoltaMarcada.Application`, `VoltaMarcada.Infrastructure`, `Microsoft.NET.Test.Sdk`, `xunit`, and `xunit.runner.visualstudio`.

Derive each GUID from SHA-256 of `state:{UF}` for states and `city:{UF}:{normalized municipality name}` for cities. Use a canonical lowercase, accent-insensitive source string before hashing so re-running generation produces identical GUIDs.

- [ ] **Step 2: Fetch and validate catalog data during generation**

The script must request the 27 state records, then request municipalities for each state. It must stop with an error unless there are exactly 27 states and at least 5,000 cities, and it must reject duplicate `(UF, city name)` pairs.

- [ ] **Step 3: Generate `LocationSeedData.cs`**

Write only English C# identifiers and emit fixed `StateSeed` / `CitySeed` records ordered by state abbreviation then city name. The checked-in file must contain no network calls.

- [ ] **Step 4: Add catalog shape tests**

```csharp
[Fact]
public void Cities_ShouldContainOnlyKnownStates()
{
    var stateIds = LocationSeedData.States.Select(state => state.Id).ToHashSet();

    Assert.All(LocationSeedData.Cities, city => Assert.Contains(city.StateId, stateIds));
}

[Fact]
public void States_ShouldContainAllBrazilianFederativeUnits()
{
    Assert.Equal(27, LocationSeedData.States.Count);
    Assert.Contains(LocationSeedData.States, state => state.Abbreviation == "DF");
}
```

### Task 3: Persist reference data and transition company schema

**Files:**

- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/StateConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CityConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/*AddCompanyLocationsAndLogo.cs`

**Consumes:** `State`, `City`, `LocationSeedData`, and the changed `Company` model.

**Produces:** Database tables `States` and `Cities`; `Companies.StateId`, `Companies.CityId`, and `Companies.PostalCode` foreign-key columns.

- [ ] **Step 1: Add `DbSet<State>` and `DbSet<City>`**

```csharp
public DbSet<State> States => Set<State>();
public DbSet<City> Cities => Set<City>();
```

- [ ] **Step 2: Configure state and city constraints**

Configure `States.Abbreviation` as required, maximum length two, and unique. Configure `Cities.StateId` as required with restrictive deletion; add a unique index named `UX_Cities_StateId_Name` on `(StateId, Name)`.

- [ ] **Step 3: Configure company location relations**

Map required `PostalCode` at maximum length eight and required `StateId` / `CityId`. Add restrictive foreign keys to `State` and `City`. Remove EF mappings for `Phone`, `City`, and `State` only after the migration safely preserves legacy columns.

- [ ] **Step 4: Scaffold and inspect `AddCompanyLocationsAndLogo`**

Generate a migration that inserts all records from `LocationSeedData`, adds nullable transition columns, maps matching legacy `State` / `City` values to IDs, then makes the new company columns non-null only after all existing rows are mapped. Do not drop unmatched legacy fields in this migration.

- [ ] **Step 5: Add integration tests for mappings**

Assert the state abbreviation unique index, city `(StateId, Name)` unique index, and restrictive company foreign keys through the configured EF model.

### Task 4: Add application contracts and transactional onboarding behavior

**Files:**

- Create: `src/VoltaMarcada.Application/Abstractions/Storage/ICompanyLogoStorage.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/ICompanyOnboardingRepository.cs`
- Create: `src/VoltaMarcada.Application/CompanyLocations/GetOnboardingStatesUseCase.cs`
- Create: `src/VoltaMarcada.Application/CompanyLocations/GetOnboardingCitiesUseCase.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommand.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommandValidator.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingResult.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Create: `tests/VoltaMarcada.Application.Tests/CompleteCompanyOnboardingUseCaseTests.cs`

**Consumes:** Domain model and persistence mappings from Tasks 1 and 3.

**Produces:**

```csharp
public sealed record CompanyLogoUpload(Stream Content, string FileName, string ContentType, long Length);
public sealed record CompanyLogoStorageResult(CompanyLogoStorageResultCode Code, string? LogoKey);
public enum CompanyLogoStorageResultCode { Success, InvalidFile }
public interface ICompanyLogoStorage
{
    Task<CompanyLogoStorageResult> SaveAsync(CompanyLogoUpload upload, CancellationToken cancellationToken);
    Task DeleteAsync(string logoKey, CancellationToken cancellationToken);
}

public Task<bool> IsCityInStateAsync(Guid cityId, Guid stateId, CancellationToken cancellationToken);
```

- [ ] **Step 1: Write failing ownership tests**

```csharp
[Fact]
public async Task ExecuteAsync_ShouldReturnInvalidLocation_WhenCityDoesNotBelongToState()
{
    var result = await useCase.ExecuteAsync(commandWithMismatchedLocation, CancellationToken.None);

    Assert.Equal(CompleteCompanyOnboardingResultCode.InvalidLocation, result.Code);
}
```

- [ ] **Step 2: Extend the command and validator**

Add `WhatsApp`, `PostalCode`, `StateId`, `CityId`, and optional `CompanyLogoUpload`. Require all non-logo fields, validate CEP with `BrazilianPostalCode.TryNormalize`, and reject empty state/city IDs.

- [ ] **Step 3: Implement explicit location read use cases**

`GetOnboardingStatesUseCase.ExecuteAsync` returns immutable `StateOption(Guid Id, string Name, string Abbreviation)` records ordered by abbreviation. `GetOnboardingCitiesUseCase.ExecuteAsync(Guid stateId, CancellationToken)` returns `CityOption(Guid Id, string Name)` records ordered by name and returns an empty list for an empty state ID.

- [ ] **Step 4: Implement safe logo coordination**

Validate state/city ownership before file storage. If a logo exists, save it and map `InvalidFile` to `InvalidLogo`. On success, create company plus owner membership in the existing transaction, then delete the saved logo when the persistence result is not `Success` or an unexpected failure is thrown.

- [ ] **Step 5: Add focused application tests**

Cover city/state mismatch, invalid CEP, successful no-logo onboarding, valid-logo onboarding that uses the returned key, rejected file outcome, and cleanup after duplicate-document persistence failure.

### Task 5: Implement Infrastructure repositories and local logo storage

**Files:**

- Modify: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CompanyOnboardingRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/CompanyLocationRepository.cs`
- Create: `src/VoltaMarcada.Infrastructure/Storage/LocalCompanyLogoStorage.cs`
- Create: `src/VoltaMarcada.Infrastructure/Storage/CompanyLogoStorageOptions.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`

**Consumes:** Application contracts from Task 4 and `IHostEnvironment` path configured by the Web host.

**Produces:** A locally stored logo under `wwwroot/uploads/company-logos/{guid}.{extension}` and tenant-safe location read/query implementations.

- [ ] **Step 1: Implement repository location queries**

Use `AsNoTracking`, order state rows by `Abbreviation`, city rows by `Name`, and filter cities by `StateId`. Implement `IsCityInStateAsync` as one SQL query filtered by both GUIDs.

- [ ] **Step 2: Implement file validation and storage**

Accept only `image/png`, `image/jpeg`, and `image/webp`; map them to `.png`, `.jpg`, and `.webp`; reject files longer than `5 * 1024 * 1024` bytes and empty uploads. Verify PNG, JPEG, and WebP magic bytes before writing. Generate `Guid.NewGuid().ToString("N") + extension`, create the directory if missing, and persist through `FileStream` with asynchronous copy.

- [ ] **Step 3: Implement safe deletion**

Reject a `logoKey` that is not rooted at `uploads/company-logos/`. Resolve only under the configured web-root upload directory and delete only the specific file when it exists.

- [ ] **Step 4: Register dependencies**

Register `ICompanyLogoStorage` and the location repository as scoped services. Define `CompanyLogoStorageOptions.RootPath` in Infrastructure and configure it in the Web composition root from `IWebHostEnvironment.WebRootPath`; do not place web-host dependencies in Application.

### Task 6: Update multipart endpoint and onboarding UI

**Files:**

- Modify: `src/VoltaMarcada.Web/Endpoints/CompanyOnboardingEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/CompanyOnboarding.razor`
- Modify: `src/VoltaMarcada.Web/wwwroot/app.css`
- Create: `tests/VoltaMarcada.Application.Tests/CompanyLogoUploadTests.cs`

**Consumes:** Tasks 4 and 5.

**Produces:** An antiforgery-protected multipart upload form with CEP, state/city dependent selects, Portuguese feedback, and preserved `returnUrl`.

- [ ] **Step 1: Bind a multipart request without tenant fields**

```csharp
public sealed record CompleteCompanyOnboardingRequest(
    string LegalName,
    string TradeName,
    string Document,
    string WhatsApp,
    string PostalCode,
    Guid StateId,
    Guid CityId,
    IFormFile? Logo,
    string? ReturnUrl);
```

Convert `Logo` into `CompanyLogoUpload` within the endpoint, never include `CompanyId` or `UserId`, and map `InvalidLocation`, `InvalidPostalCode`, and invalid-logo outcomes to localized errors.

- [ ] **Step 2: Render the form fields**

Set `enctype="multipart/form-data"`. Remove telephone and logo-key text inputs. Add `input type="file"` with `accept="image/png,image/jpeg,image/webp"`, a CEP text input, a state select, and a city select.

- [ ] **Step 3: Implement dependent dropdown behavior**

Load states at initialization. When state changes, clear city selection, load only that state's cities, and disable the city select while no state is selected. Keep names `stateId` and `cityId` in the final HTML form so normal post binding works.

- [ ] **Step 4: Preserve redirects and responsive layout**

Keep the hidden `returnUrl`. All error redirects must retain a normalized local return URL; successful onboarding must redirect to its normalized return URL. Update the onboarding grid so CEP, WhatsApp, state, city, and logo remain accessible on narrow screens.

### Task 7: Review generated schema and complete verification

**Files:**

- Modify only files required by defects found during verification.

- [ ] **Step 1: Inspect generated migration content**

Confirm the migration has exactly 27 state inserts, more than 5,000 city inserts, state/city company foreign keys, CEP mapping, and no runtime HTTP calls.

- [ ] **Step 2: Run static checks permitted by repository rules**

Run `git diff --check` and inspect every changed form field name against the endpoint request contract.

- [ ] **Step 3: Request explicit permission before executable verification**

Ask before running `dotnet build`, test suites, database migrations, or the web app. When permission is granted, verify registration, state/city dependency, valid and rejected logos, successful onboarding, return URL preservation, and a platform-administrator login.

## Plan Self-Review

- Data model, catalog inserts, CEP, WhatsApp-only contact, upload storage, state/city dependent UI, server-side ownership validation, `returnUrl`, and verification each have dedicated tasks.
- The plan uses fixed GUIDs for reference data and no external runtime dependency.
- All types consumed by later tasks are defined in prior tasks.
- No task omits a validation condition, redirect behavior, or file storage cleanup path.
