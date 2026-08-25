# Company Onboarding Design

## Goal

Implement Phase 1 authentication and first-company onboarding for VoltaMarcada. A person can create an account, authenticate with ASP.NET Core Identity, register one company, and enter the authenticated dashboard. Every user who can access company functionality must belong to a company.

## Scope

Included:

- Public account registration and login.
- Automatic authentication after successful account registration.
- Required company onboarding for authenticated users without a company membership.
- Company creation and owner membership creation.
- Current user and current company resolution from the authenticated identity.
- Authenticated dashboard placeholder and route protection.
- Company persistence, constraints, and migration.

Excluded:

- Employee management.
- Invitations.
- Email confirmation.
- Logo upload or external storage integration.
- Automated tests for this phase, by product decision.

## Architecture

### Identity and membership

`ApplicationUser` remains the ASP.NET Core Identity user. It will not have a direct `CompanyId`, because membership is represented exclusively by `CompanyUser` and must not have two sources of truth.

`CompanyUser` is the explicit link between an Identity user and a company. It contains `UserId`, `CompanyId`, and `Role`. The only role created in this phase is `Owner`; future employee and invitation flows will create other memberships.

An account may exist without a `CompanyUser` only during the initial onboarding transition. It is not allowed to access the dashboard or other internal company routes until onboarding completes.

### Company

`Company` is the tenant boundary and contains:

- `Id`
- `LegalName`
- `TradeName`
- `Document`
- `Phone`
- `WhatsApp`
- `City`
- `State`
- `LogoKey` (optional external-storage key)
- `Slug`
- `CreatedAtUtc`
- `UpdatedAtUtc`

All properties except `LogoKey` are required. The entity protects its state and validates its basic invariant requirements.

### Application behavior

`CompleteCompanyOnboarding` is an explicit application use case. It receives only company registration data; it resolves the authenticated identity from `ICurrentUser`, never from the browser. In one database transaction it:

1. Validates the request.
2. Checks document and slug availability.
3. Creates the company.
4. Creates the `CompanyUser` membership with role `Owner`.
5. Persists both records.

`ICurrentCompany` resolves the company through the authenticated user membership. Future tenant-scoped use cases use it rather than accepting a client-provided `CompanyId`.

## User flow and routes

1. A visitor registers at `/account/register` with email, password, and password confirmation.
2. Identity creates the account and signs in the user. Email confirmation is not required in this phase.
3. The user is redirected to `/onboarding/company`.
4. The user submits the company form. On success, the application creates the company and its `Owner` membership atomically.
5. The user is redirected to `/dashboard`.
6. A user who signs in at `/account/login` is redirected to `/dashboard` when a membership exists; otherwise they are redirected to `/onboarding/company`.
7. Unauthenticated users are redirected to login. Authenticated users without a membership are prevented server-side from reaching internal company pages and sent to onboarding.

Every protected page must preserve its requested local path in `returnUrl` when redirecting a user to login or onboarding. Redirect targets must be validated as local paths before use; external URLs, protocol-relative URLs, and invalid paths fall back to `/dashboard`.

The dashboard is intentionally a minimal authenticated placeholder for the next phases.

## Validation and persistence rules

- `Document` accepts Brazilian CPF or CNPJ, is validated using check digits, and is persisted normalized to digits only.
- `Document` has a unique database index.
- `Slug` is derived from `TradeName`: lowercase, accent-insensitive, and hyphen-separated.
- When the base slug already exists, append the final four normalized document digits, for example `company-name-1234`.
- `Slug` has a unique database index.
- The company form requires legal name, trade name, CPF/CNPJ, phone, WhatsApp, city, and state. `LogoKey` is optional.
- A unique membership index prevents duplicate `CompanyUser` links for the same user and company.
- Expected conflicts such as an existing document or slug produce localized form feedback, not technical exception messages.

## Security

- Password management and authentication cookies are provided by ASP.NET Core Identity.
- UI visibility is not treated as authorization. Protected routes and application behavior enforce authentication and membership server-side.
- Authenticated operations do not accept `CompanyId` from the client. The company is resolved from the current authenticated user.
- The application does not log passwords, normalized documents, session data, or other sensitive onboarding data.
- Foreign keys use restrictive deletion behavior to preserve tenant ownership integrity.

## Delivery and verification

- Add a descriptive EF Core migration for the company and membership model.
- No automated tests will be implemented in this phase by explicit product decision.
- Verify the implementation by building the solution and manually exercising registration, login, onboarding, dashboard access, protected-route redirects, duplicate document handling, and slug collision handling.
