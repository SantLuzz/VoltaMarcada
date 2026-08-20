# Professionals and Operational Access Design

## Goal

Introduce company professionals linked to employees, define the services each professional can execute, and establish owner-managed, read-only operational records for employees.

## Scope

The change adds a tenant-isolated Professionals module and replaces physical deletion in the existing Customer, Equipment, and Service modules with logical deactivation. It also makes those three existing modules read-only for company employees.

Work orders, appointments, availability, schedule blocks, public booking, and historical operational records remain outside this scope.

## Authorization Model

The Company Owner exclusively manages Phase 2 operational configuration:

- customers;
- equipment;
- services;
- professionals.

An Employee can consult Customers, Equipment, and Services, but cannot create, update, deactivate, reactivate, or delete them. The restriction is enforced in both the rendered UI and server-side endpoints; direct navigation and crafted requests cannot bypass it.

For the Professionals menu entry, the same route resolves differently by membership role:

- an Owner is shown the professionals management list;
- an Employee is shown only the professional record linked to their authenticated CompanyMember, with every field read-only;
- an Employee without a linked professional sees a clear non-linked-profile state and no management actions.

## Professionals Domain Model

`Professional` is a company-scoped entity with server-generated `Guid` identity. It has:

- `CompanyId`;
- mandatory `CompanyUserId` link to an accepted CompanyMember;
- `IsActive` status;
- creation and update UTC timestamps;
- selected services through `ProfessionalService` links.

The CompanyUser link is unique per company: one CompanyMember can be associated with at most one Professional. The linked member supplies the displayed employee identity; Professional does not duplicate or update any employee account/profile data.

`ProfessionalService` is a company-scoped relationship between one Professional and one Service. A professional can perform multiple services, and services can be assigned to multiple professionals. Only services active in the current company are selectable when a professional is created or updated.

Creating and editing a Professional requires a valid current-company CompanyMember and a distinct list of valid active current-company Service IDs. Updating or changing status on Professional never modifies CompanyUser or ApplicationUser data.

## Logical Deactivation

Customers, Equipment, Services, and Professionals use `IsActive` instead of physical removal.

- Existing delete actions become explicit **Inativar** actions.
- The Owner can reactivate a record.
- Lists show the active/inactive status to Owners and continue to include both states for management.
- Operational selection queries return active records by default, preventing deactivated records from appearing in new assignments.
- Existing rows are migrated as active.

The physical-delete application use cases and UI wording are removed or replaced so that the action cannot imply data erasure. Entity-specific dependent data is retained, including service checklist items and professional-service links.

## Application and Persistence

The Professionals module follows the established explicit-use-case pattern:

- `CreateProfessionalUseCase`;
- `GetProfessionalsUseCase`;
- `GetProfessionalForEditUseCase`;
- `GetCurrentEmployeeProfessionalUseCase`;
- `UpdateProfessionalUseCase`;
- `ChangeProfessionalStatusUseCase`.

Dedicated commands, DTOs, operation results, validators, and a narrowly scoped repository abstraction support these operations. All read and write predicates filter on `CompanyId`; entity IDs alone never select tenant-owned data.

EF Core maps `Professionals` and `ProfessionalServices` via individual Fluent configuration classes. The schema adds tenant and relationship indexes, a unique `CompanyId`/`CompanyUserId` constraint, and restrictive foreign keys that preserve the tenant boundary. A descriptive migration adds the module and `IsActive` fields for Customers and Equipment; existing Service status is reused.

Owner-only mutation is enforced at endpoint authorization and revalidated in the application use cases through the current authenticated user and company membership context.

## Web Experience

The authenticated menu gains **Profissionais**. The Owner sees a searchable management list with employee name, selected-service summary, status, and actions to edit or change status. The registration screen uses the established list-and-form pattern:

- mandatory employee dropdown, limited to valid current-company members and excluding members already linked to another professional;
- mandatory multi-select dropdown of active services;
- active status control when editing;
- clear action feedback and status confirmation dialogs.

The linked employee fields are not editable on this screen. An Employee's own professional page uses the same information layout but controls are disabled and no action buttons are rendered.

Customer, Equipment, and Service lists hide Owner-only mutation controls for Employees. Existing registration pages detect Employee access and render the loaded fields disabled, with no submit actions. The Owner retains normal management actions, now using status changes instead of deletion.

## Error Handling and Security

Expected conditions use explicit operation results: missing record in the current company, invalid linked member, duplicate professional-member link, unavailable service selection, and authorization failure. UI errors use existing localized patterns. Internal details are logged without exposing tenant or personal data.

## Verification

No automated tests are added because this is MVP validation work and tests were not requested. Validation will be static: review domain invariants, application ownership checks, tenant-safe query predicates, Fluent mappings and migration intent, endpoint authorization, and the Owner/Employee UI paths. No build, test run, or application launch will occur unless requested.

## Assumptions

- The existing CompanyUser record represents the employee/CompanyMember selected in the Professional screen.
- All accepted members, including an Owner if relevant to the business, can be linked as a Professional; Employee-only redirection is based on the authenticated membership role.
- A Professional must have at least one assigned service because the requested registration exists to identify services the professional executes.
