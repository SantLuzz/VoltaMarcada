# Owner Company Area and Employee Invitations Design

## Goal

Allow a company owner to view company information, pre-register employees, and share secure invitation links that let employees create their VoltaMarcada account and join the company with the `Employee` role.

## Scope

This design covers the missing Phase 1 employee-onboarding capability and the owner-only company area that hosts it.

It includes:

- An owner-only company page at `/empresa`.
- A sidebar item that is rendered only for company owners.
- Read-only company information on that page.
- Basic employee pre-registration by the owner.
- Secure, copyable invitation links without email delivery.
- Public invitation completion that creates an Identity user and a `CompanyUser` membership with the `Employee` role.
- Active employee and invitation history views for the owner.

It deliberately excludes editing company information, employee profile editing, employee removal, email delivery, and professional scheduling data.

## User Flows

### Owner creates an employee invitation

1. An authenticated company owner opens `/empresa` from the sidebar.
2. The owner sees the current company information in read-only form, active employees, and invitations.
3. The owner submits the employee's full name, email address, CPF, phone number, and free-form address.
4. The system validates the input, creates a pending invitation, and presents a copyable secure link.
5. The owner independently chooses how to deliver the link.

### Employee completes an invitation

1. The employee opens the invitation URL.
2. The public page validates the token before displaying any context.
3. It presents the employee name and a masked version of the pre-registered email address, then requests password and password confirmation only.
4. On submission, the server revalidates the invitation, creates the Identity user with the pre-registered email as user name and email, creates the `CompanyUser` membership with role `Employee`, and consumes the invitation atomically.
5. The employee is signed in and redirected to the dashboard.

## Data Model

### EmployeeInvitation

Create a domain entity named `EmployeeInvitation` with the following state:

- `Id` (`Guid`)
- `CompanyId` (`Guid`)
- `FullName` (`string`)
- `Email` (`string`, normalized for comparisons)
- `Document` (`string`, normalized CPF)
- `Phone` (`string`)
- `Address` (`string`)
- `TokenHash` (`string`)
- `ExpiresAtUtc` (`DateTimeOffset`)
- `CreatedAtUtc` (`DateTimeOffset`)
- `CreatedByUserId` (`Guid`)
- `AcceptedAtUtc` (`DateTimeOffset?`)
- `RevokedAtUtc` (`DateTimeOffset?`)

The raw token is generated with a cryptographically secure random source, shown only when the invitation is created or renewed, and never persisted. The database stores only its cryptographic hash.

The invitation is pending only while it is not accepted, not revoked, and not expired. Its default expiration is seven days after creation.

`EmployeeInvitation` provides behavior to create an invitation, verify whether it can be accepted, consume it, and revoke it. A renewed invitation is a newly generated pending invitation; the previous pending invitation for the same employee is revoked.

The existing `CompanyUser` model remains the authoritative tenant membership record. It is created only after an invitation is successfully accepted, preserving the existing non-null `UserId` relationship.

## Authorization and Tenant Isolation

Introduce a `CompanyOwner` authorization policy. It requires an authenticated user to have a `CompanyUser` record for the resolved current company with role `Owner`.

The `/empresa` route and every internal employee-invitation endpoint require `CompanyOwner`. The sidebar may hide the link from employees, but it is not an authorization boundary.

All owner operations resolve the company from `ICurrentCompany`; they never accept a `CompanyId` from the route, query string, form, or body. Invitation queries and mutations filter by the resolved `CompanyId`.

The invitation completion endpoints are anonymous but identify an invitation exclusively through a non-guessable token. They expose only the minimum required context: employee name and masked email address.

## Validation and Conflict Handling

The owner form requires full name, valid email address, valid CPF, phone number, and address.

Creating an invitation fails with a user-facing validation result when:

- The email already belongs to an Identity user.
- An active company membership already uses the email.
- A pending invitation in the current company already uses the email or CPF.
- An accepted invitation in the current company already uses the CPF, preserving one employee record per document within the company.

The invitation completion flow fails safely when the token is absent, malformed, expired, revoked, accepted, or no longer valid. It also detects an email that became registered after the invitation was generated.

No operation reveals private invitation information to a caller without the token or owner authorization.

## Persistence

Add an EF Core configuration and migration for `EmployeeInvitation`.

Indexes support the expected access patterns:

- Unique index on `TokenHash`.
- Index on `CompanyId` and creation timestamp for the owner history.
- Index on `CompanyId`, email, and invitation state fields for pending-invitation validation.
- Index on `CompanyId`, document, and invitation state fields for pending-invitation validation.

Database naming follows the solution's existing convention for this migration and its table mappings.

## Application and Web Components

Application use cases remain explicit and tenant-aware:

- `CreateEmployeeInvitationUseCase`
- `GetCompanyDetailsUseCase`
- `GetEmployeesAndInvitationsUseCase`
- `RenewEmployeeInvitationUseCase`
- `RevokeEmployeeInvitationUseCase`
- `AcceptEmployeeInvitationUseCase`

The Application project defines the persistence and Identity abstractions it needs. Infrastructure implements them with EF Core and ASP.NET Core Identity. The Web project only maps HTTP forms/endpoints and renders Razor components; it does not contain invitation business rules.

The `/empresa` page has three plain sections: company information, employee pre-registration, and employees/invitations. It uses the existing MudBlazor visual language without adding a new frontend framework or elaborate UI behavior.

The public invitation page is separate from the standard public registration page. It has a route shaped as `/convites/funcionarios/{token}` and does not offer company onboarding.

The existing generic registration flow continues to lead users without a membership to company onboarding. It does not create employee memberships.

## Concurrency and Consistency

Accepting an invitation is concurrency-sensitive. The implementation rechecks the invitation's state and the Identity email availability immediately before committing. The Identity user creation, `CompanyUser` creation, and invitation consumption run in one database transaction so a token cannot produce multiple accounts or a partial membership.

The owner can revoke a pending invitation. If a link is opened before revocation but submitted afterward, the final server-side state check rejects it.

## Verification

The repository's current MVP rule does not require automated tests unless explicitly requested. Changes will therefore be reviewed statically and the generated migration will be inspected for correct schema, indexes, constraints, authorization, tenant filtering, and public-token handling. No build, test run, or application launch will be performed unless explicitly requested.
