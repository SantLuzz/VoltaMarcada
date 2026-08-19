# Cloudinary File Storage Design

## Purpose

Replace local company-logo persistence under `wwwroot` with Cloudinary storage while keeping application behavior independent from the provider.

## Architecture

`ServiceSync.Application` will own an `IFileStorage` contract because application use cases depend on storage behavior, while `ServiceSync.Infrastructure` will provide `CloudinaryFileStorage`.

The contract will support two operations:

- Upload a file stream with its content type and a logical folder, returning the provider key.
- Delete an uploaded object by its key.

The contract will not return or persist public URLs. The company already stores `LogoKey`; it remains the only stored logo reference.

## Cloudinary Behavior

The Cloudinary implementation will:

- validate the existing logo restrictions: PNG, JPEG, or WebP with a maximum size of 5 MB;
- upload logos as images to the `company-logos` folder;
- generate a non-guessable provider key and return Cloudinary's public identifier as that key;
- delete by that public identifier using the image resource type;
- derive public URLs from the environment-configured Cloudinary base URL when a consumer needs to render an image.

Cloudinary credentials will be read from the existing `Cloudinary` configuration section. The configuration will be moved to Infrastructure so the provider implementation owns its settings. No secret values will be added to tracked configuration.

## Integration

`CompleteCompanyOnboardingUseCase` will use `IFileStorage` for the logo upload and retain its existing rollback: if company persistence fails after upload, the uploaded Cloudinary object is deleted using its returned key.

The local `wwwroot/uploads/company-logos` implementation and its DI registration will be removed. Static product assets such as the official ServiceSync logo remain under `wwwroot` and are out of scope.

## Error Handling and Verification

Invalid logo files continue to map to the existing `InvalidLogo` onboarding result. Provider failures are not converted into business validation errors and will surface through normal unexpected-error handling.

Unit tests will cover the contract-facing onboarding flow and Cloudinary request behavior through the SDK boundary where practical. The project instructions prohibit builds and test runs unless explicitly requested, so verification will use static inspection unless that authorization is provided.
