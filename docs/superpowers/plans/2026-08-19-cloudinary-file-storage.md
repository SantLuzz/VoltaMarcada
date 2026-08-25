# Cloudinary File Storage Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace local company-logo files with provider-agnostic Cloudinary storage that persists only Cloudinary public IDs.

**Architecture:** Application owns a generic `IFileStorage` contract and file DTOs. Infrastructure implements it through `CloudinaryDotNet`; onboarding validates the image policy, uploads to the `company-logos` folder, persists the returned key, and compensates with a delete if database persistence fails.

**Tech Stack:** .NET 10, C#, ASP.NET Core, CloudinaryDotNet 1.29.2, FluentValidation.

**Spec:** `docs/superpowers/specs/2026-08-19-cloudinary-file-storage-design.md`

## Global Constraints

- All source identifiers and technical messages are English; no credentials are stored in source control.
- `Company.LogoKey` remains the only stored company-logo reference; URLs are derived later from environment configuration.
- Do not run `dotnet build`, `dotnet test`, the web host, migrations, or database commands unless the user explicitly asks.
- Do not add automated tests in this task because the repository instructions permit them only when explicitly requested.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| `src/VoltaMarcada.Application/Abstractions/Storage/IFileStorage.cs` | Provider-neutral upload/delete contract and DTOs. |
| `src/VoltaMarcada.Application/CompanyOnboarding/*` | Logo-policy validation and storage orchestration. |
| `src/VoltaMarcada.Infrastructure/Storage/CloudinaryFileStorage.cs` | Cloudinary SDK upload/delete implementation. |
| `src/VoltaMarcada.Infrastructure/Storage/CloudinaryOptions.cs` | Infrastructure-owned provider configuration. |
| `src/VoltaMarcada.Infrastructure/DependencyInjection.cs` | Cloudinary client and `IFileStorage` registration. |
| `src/VoltaMarcada.Infrastructure/VoltaMarcada.Infrastructure.csproj` | Cloudinary SDK package reference. |
| `src/VoltaMarcada.Web/Program.cs` | Remove provider configuration owned by Infrastructure. |
| `src/VoltaMarcada.Web/Configuration/CloudinaryOptions.cs` | Remove obsolete Web-owned options type. |
| `src/VoltaMarcada.Web/appsettings.json` | Declare non-secret Cloudinary configuration names. |
| `src/VoltaMarcada.Infrastructure/Storage/LocalCompanyLogoStorage.cs` | Remove obsolete `wwwroot` persistence. |
| `src/VoltaMarcada.Application/Abstractions/Storage/ICompanyLogoStorage.cs` | Remove replaced logo-specific contract. |

### Task 1: Replace the logo-specific application contract

**Files:**

- Create: `src/VoltaMarcada.Application/Abstractions/Storage/IFileStorage.cs`
- Delete: `src/VoltaMarcada.Application/Abstractions/Storage/ICompanyLogoStorage.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingCommand.cs`
- Modify: `src/VoltaMarcada.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`

**Produces:**

```csharp
public sealed record FileUpload(Stream Content, string FileName, string ContentType, long Length);
public sealed record FileStorageUploadRequest(FileUpload File, string Folder);
public sealed record FileStorageUploadResult(string Key);

public interface IFileStorage
{
    Task<FileStorageUploadResult> UploadAsync(FileStorageUploadRequest request, CancellationToken cancellationToken);
    Task DeleteAsync(string key, CancellationToken cancellationToken);
}
```

- [ ] **Step 1: Create `IFileStorage` and its DTOs**

Place the definitions above in `VoltaMarcada.Application.Abstractions.Storage`. Require the storage result key to be non-null through the positional record parameter.

- [ ] **Step 2: Keep the endpoint-owned upload representation**

Change the command logo type from `CompanyLogoUpload?` to `FileUpload?`. The endpoint will continue constructing this type from `IFormFile`; Application remains independent of ASP.NET Core.

- [ ] **Step 3: Move logo policy into onboarding orchestration**

Inject `IFileStorage` into `CompleteCompanyOnboardingUseCase`. Before upload, accept only `image/png`, `image/jpeg`, and `image/webp`, and reject lengths outside `1..5 * 1024 * 1024` by returning the existing `InvalidLogo` result.

```csharp
private static bool IsValidLogo(FileUpload logo) =>
    logo.Length is > 0 and <= 5 * 1024 * 1024 &&
    logo.ContentType is "image/png" or "image/jpeg" or "image/webp";
```

- [ ] **Step 4: Upload and compensate by returned key**

For a valid logo, call:

```csharp
var upload = await fileStorage.UploadAsync(
    new FileStorageUploadRequest(command.Logo, "company-logos"), cancellationToken);
logoKey = upload.Key;
```

Retain the existing non-success persistence rollback, calling `fileStorage.DeleteAsync(logoKey, cancellationToken)`. Do not construct or persist a URL.

### Task 2: Implement Cloudinary in Infrastructure

**Files:**

- Modify: `src/VoltaMarcada.Infrastructure/VoltaMarcada.Infrastructure.csproj`
- Create: `src/VoltaMarcada.Infrastructure/Storage/CloudinaryOptions.cs`
- Create: `src/VoltaMarcada.Infrastructure/Storage/CloudinaryFileStorage.cs`
- Delete: `src/VoltaMarcada.Infrastructure/Storage/LocalCompanyLogoStorage.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`

**Consumes:** `IFileStorage`, `FileStorageUploadRequest`, and `FileStorageUploadResult` from Task 1.

**Produces:** An `IFileStorage` that uses Cloudinary public IDs as storage keys.

- [ ] **Step 1: Reference the supported Cloudinary SDK**

Add the following package reference to the Infrastructure project:

```xml
<PackageReference Include="CloudinaryDotNet" Version="1.29.2" />
```

- [ ] **Step 2: Define provider settings in Infrastructure**

Create an options class with configuration section name `Cloudinary` and these properties:

```csharp
public sealed class CloudinaryOptions
{
    public const string SectionName = "Cloudinary";
    public string CloudName { get; init; } = string.Empty;
    public string ApiKey { get; init; } = string.Empty;
    public string ApiSecret { get; init; } = string.Empty;
    public string BaseUrl { get; init; } = string.Empty;
}
```

`BaseUrl` is retained only for future URL resolution; this storage implementation neither returns nor persists it.

- [ ] **Step 3: Implement upload using a generated public ID**

Construct `ImageUploadParams` with the request stream, original filename, content type, folder, and `PublicId = Guid.NewGuid().ToString("N")`. Use `Cloudinary.UploadAsync`, ensure the result exposes a non-empty `PublicId`, and return it as `FileStorageUploadResult.Key`. Use the returned full public ID so nested folders are included in the persisted key.

- [ ] **Step 4: Implement key-based deletion**

Use `Cloudinary.DestroyAsync(new DeletionParams(key) { ResourceType = ResourceType.Image })`. Treat Cloudinary's `not_found` response as idempotent success; throw `InvalidOperationException` for other unsuccessful results so provider failures are visible rather than misreported as invalid uploads.

- [ ] **Step 5: Register only the Cloudinary storage implementation**

In `AddInfrastructure`, bind `CloudinaryOptions`, validate non-empty `CloudName`, `ApiKey`, and `ApiSecret` when resolving the client, then register a singleton `Cloudinary` built from `new Account(cloudName, apiKey, apiSecret)` and scoped `IFileStorage` as `CloudinaryFileStorage`. Remove `ICompanyLogoStorage` and `LocalCompanyLogoStorage` registrations.

### Task 3: Update Web configuration and clean up local persistence

**Files:**

- Modify: `src/VoltaMarcada.Web/Endpoints/CompanyOnboardingEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Modify: `src/VoltaMarcada.Web/appsettings.json`
- Delete: `src/VoltaMarcada.Web/Configuration/CloudinaryOptions.cs`

**Consumes:** The Application `FileUpload` type and Infrastructure `AddInfrastructure` registration.

- [ ] **Step 1: Send the generic upload DTO from the endpoint**

Replace `new CompanyLogoUpload(...)` with:

```csharp
new FileUpload(logoStream, request.Logo.FileName, request.Logo.ContentType, request.Logo.Length)
```

Keep stream lifetime and existing localized invalid-logo response unchanged.

- [ ] **Step 2: Remove Web-owned Cloudinary binding**

Remove `using VoltaMarcada.Web.Configuration;` and the `Configure<CloudinaryOptions>` call from `Program.cs`. `AddInfrastructure(builder.Configuration)` becomes the sole composition point for Cloudinary configuration.

- [ ] **Step 3: Align environment configuration keys**

Replace `Cloudinary:BaseUrl`, `Cloudinary:ApiKey`, and `Cloudinary:ApiSecret` placeholders with `CloudName`, `ApiKey`, `ApiSecret`, and optional `BaseUrl`. Keep all values empty in tracked `appsettings.json`; real values remain in environment variables or development secrets.

### Task 4: Static verification

**Files:**

- Modify only files necessary to correct defects discovered below.

- [ ] **Step 1: Inspect provider boundary**

Confirm Application references neither `CloudinaryDotNet` nor `wwwroot`, while Infrastructure is the only project that references the Cloudinary SDK.

- [ ] **Step 2: Inspect data and cleanup behavior**

Confirm `Company.Create` still receives `logoKey`, no URL property is added, and every post-upload non-success onboarding result deletes by the stored Cloudinary public ID.

- [ ] **Step 3: Run allowed static checks**

Run `git diff --check` if Git safety configuration allows it, then search changed source for `LocalCompanyLogoStorage`, `ICompanyLogoStorage`, and `uploads/company-logos`; no production references may remain. Do not run build, test, migration, or host commands without explicit permission.

## Plan Self-Review

- The generic application contract, Cloudinary upload/delete implementation, configuration ownership, existing onboarding rollback, and removal of local `wwwroot` persistence each have a task.
- `LogoKey` is the only persisted value; `BaseUrl` is configuration-only for future resolution.
- No database migration is required because `LogoKey` already stores a string key.
- No automated tests are added because the repository instructions explicitly prohibit adding them unless requested.
