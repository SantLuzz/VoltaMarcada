# Company Location and Logo Design

## Goal

Refine company onboarding so a company has a WhatsApp contact, a Brazilian postal code, a selected state and city, and an optional uploaded logo. State and city selections must use locally persisted Brazilian reference data and must not rely on an external lookup service at onboarding time.

## Scope

Included:

- Replace the company phone field with WhatsApp as the single contact field.
- Add a postal code to the company profile.
- Add `State` and `City` reference entities and link the company to both.
- Seed all Brazilian states, the Federal District, and municipalities through a deterministic database migration.
- Replace free-text state and city inputs with dependent dropdowns.
- Accept a logo image upload, save it locally with a GUID-based file name, and persist its relative path in `LogoKey`.

Excluded:

- Postal-code lookup or address completion through external services.
- External object storage integration.
- Company address lines, neighborhoods, street numbers, or geocoding.
- Public company page logo rendering.

## Data Model

### State

`State` is a shared reference entity with:

- `Id` (`Guid`)
- `Name`
- `Abbreviation`

`Abbreviation` stores the two-character Brazilian UF. It is unique.

### City

`City` is a shared reference entity with:

- `Id` (`Guid`)
- `StateId`
- `Name`

`StateId` is a required restrictive foreign key. The combination of `StateId` and `Name` is unique.

### Company

The company profile replaces free-text `City` and `State` with required `StateId` and `CityId`. It also adds required `PostalCode` and retains required `WhatsApp` and optional `LogoKey`.

`Phone` is removed from the domain and no longer accepted by the onboarding command or form. Existing database values are retained only when required for a non-destructive schema transition; no new application behavior reads or writes them.

The onboarding use case validates that the selected city belongs to the selected state before creating the company.

## Reference Data and Migration

A new migration seeds the 26 Brazilian states, the Federal District, and all Brazilian municipalities from an official IBGE-derived catalog captured during development. The generated migration contains deterministic inserts and fixed GUIDs, so applying it never needs network access.

The migration adds the company foreign-key columns and postal code. When existing text-based company location values match the seeded catalog, the migration maps them to the new IDs. Legacy text columns are not dropped until a later audited cleanup migration, preventing loss of unmatched historical data.

## Logo Storage

The onboarding form uses `multipart/form-data` and sends an optional logo file. The accepted formats are PNG, JPEG, and WebP, with a maximum size of 5 MB.

The application exposes an `ICompanyLogoStorage` abstraction. The initial Infrastructure implementation:

1. Validates the supplied file extension and content type.
2. Generates a GUID file name while preserving only an allowed extension.
3. Saves the file under `wwwroot/uploads/company-logos`.
4. Returns the relative path stored in `Company.LogoKey`.

The onboarding flow compensates for a failed database transaction by deleting a file that was saved but could not be linked to a company. This interface can later be implemented by external object storage without changing the application use case.

## Onboarding UI and Flow

The company form contains legal name, trade name, CPF/CNPJ, WhatsApp, postal code, state, city, and optional logo file.

State is selected first. The city dropdown is disabled until a state is selected, then is populated only with cities that belong to that state. The state and city selection uses the existing interactive Blazor rendering while final onboarding submission remains a normal antiforgery-protected HTML form.

No CEP lookup is performed in this iteration. The postal code is validated and normalized server-side to eight digits.

The current `returnUrl` is preserved through validation failures and successful onboarding. It remains normalized through `LocalReturnUrl` before every redirect.

## Security and Error Handling

- The authenticated user is still resolved exclusively through `ICurrentUser`.
- The browser cannot choose a company ID, user ID, or a city belonging to a different state.
- Uploaded files are never served by their original names.
- Invalid file formats, oversized uploads, invalid postal codes, missing state/city selections, or city/state mismatch return localized onboarding feedback.
- The platform administrator remains exempt from company onboarding and does not receive a company membership.

## Verification

Tests should cover postal-code normalization, state/city ownership validation, file validation, GUID-based logo paths, and cleanup when persistence fails. Integration tests should validate the state/city foreign keys, the full seed data, and tenant-safe onboarding.

Manual verification should cover dependent dropdown behavior, a valid logo upload, rejected invalid and oversized files, preserved return URLs, a successful company creation, and login after onboarding.
