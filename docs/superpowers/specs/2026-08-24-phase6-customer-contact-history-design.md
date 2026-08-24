# Phase 6 Customer Contact History Design

## Goal

Turn a maintenance opportunity contact into a tenant-scoped commercial record that preserves the prepared WhatsApp message, the intent to contact, and explicit confirmation that the company sent it.

## Scope

This phase is the user-requested Phase 6 and supersedes the previously proposed phase ordering only for contact history. It includes manual, assisted WhatsApp contact for maintenance opportunities and does not include public booking, contextual public tokens, a WhatsApp Cloud API integration, delivery receipts, inbound messages, background automation, or a general CRM timeline.

## Existing Flow

The owner-only maintenance center currently has a WhatsApp button that marks an opportunity as contacted and opens a browser-generated WhatsApp link. That action leaves no record of the prepared message or whether the company confirmed sending it. `MaintenanceOpportunity` already owns the commercial status transition from `Pending` to `Contacted`.

## Domain Model

### CustomerContactChannel

```csharp
public enum CustomerContactChannel
{
    WhatsAppLink
}
```

The enum deliberately leaves room for future channels, including `WhatsAppCloudApi`, without adding an API integration now.

### CustomerContactStatus

```csharp
public enum CustomerContactStatus
{
    Prepared,
    Sent
}
```

`Prepared` means ServiceSync prepared and opened a WhatsApp link for a company user. It never claims the message reached WhatsApp or the customer. `Sent` means that user explicitly confirmed the company sent it. There is no delivered/read state in the MVP.

### CustomerContact

`CustomerContact` is a company-scoped domain entity with:

- `Id`, `CompanyId`, `CustomerId`, and `OpportunityId`.
- `Channel`, `Status`, and the required immutable `Message`.
- `CreatedAtUtc` and nullable `SentAtUtc`.

It is created through `Prepare(...)`, which validates all identifiers and a non-empty message of at most 2,000 characters. `MarkAsSent(...)` permits only a `Prepared` contact and records `SentAtUtc`. Both timestamps use `DateTimeOffset` in UTC. The entity has no public setters.

`CustomerContact` remains a separate entity rather than a child collection with side effects on `MaintenanceOpportunity`: preparing a link records an attempted contact; confirming sending is the only operation that updates the opportunity to `Contacted`.

## Persistence and Tenant Isolation

`AppDbContext` gains `DbSet<CustomerContact>` and a dedicated EF Core configuration. Each contact has restrictive foreign keys scoped by `(CompanyId, CustomerId)` and `(CompanyId, OpportunityId)`, so it cannot link records from another tenant. The opportunity already exposes a company-scoped alternate key used by this foreign key.

Indexes support the intended reads:

- `(CompanyId, OpportunityId, CreatedAtUtc)` for an opportunity history ordered by time.
- `(CompanyId, CustomerId, CreatedAtUtc)` for future customer-context history.

A descriptive `AddCustomerContacts` migration and the EF model snapshot introduce the schema. Authenticated requests resolve the company from the current user; no request accepts a company identifier.

## Application Behavior

### Prepare WhatsApp Contact

`PrepareMaintenanceOpportunityWhatsAppContactUseCase` is owner-authorized. It loads the maintenance opportunity and customer with a company filter, accepts only an open opportunity in `Pending` or `Contacted`, constructs the standard maintenance message, creates a `CustomerContact` with `WhatsAppLink` and `Prepared`, and persists it.

Message and WhatsApp URL formatting live behind a focused application abstraction such as `IWhatsAppLinkBuilder`, implemented in Infrastructure. It normalizes a valid customer WhatsApp number and URL-encodes the message in one place. The result contains the contact identifier and the fully constructed link. A missing or invalid WhatsApp number returns a feature-owned expected error; it does not create a partial contact record.

### Confirm WhatsApp Sending

`ConfirmMaintenanceOpportunityContactSentUseCase` loads the contact and its opportunity using both identifiers and the resolved company. It requires `Prepared`, marks the contact as `Sent`, and invokes the existing idempotent `MaintenanceOpportunity.MarkAsContacted(now)`. Both state changes are saved together by the same persistence operation. The result is an explicit record of the send and a consistent opportunity status.

Reconfirming a sent contact or confirming after the opportunity leaves the allowable contact state returns an explicit conflict-style operation result. Expected business failures use dedicated English error codes under `CustomerContact.*`, mapped to Brazilian Portuguese in Web.

### History Query

The existing maintenance opportunity list query reads its related contacts in one tenant-filtered repository operation and exposes a chronologically descending contact-history DTO for each listed opportunity. The history displays channel, status, prepared message, creation time, and sent time. No contact can be read from an unrelated customer or company.

## Web Experience

The owner-only `/manutencoes` page replaces the current direct WhatsApp behavior:

1. **WhatsApp** calls the prepare use case, refreshes the opportunity, and opens the returned pre-filled link.
2. The newly displayed `Prepared` history entry offers **Confirmar envio**.
3. Confirmation calls the send use case and refreshes the list; the entry shows its sent time and the opportunity becomes `Contacted`.

Each opportunity provides an expandable **Historico de contatos** section. It displays the message and timestamps without exposing internal IDs. The existing scheduling, completion, and dismissal actions remain unchanged. The obsolete untracked **Marcar contato** button is removed so no commercial WhatsApp action bypasses history.

The UI uses existing MudBlazor controls, loading prevention, centralized error messages, and Brazilian Portuguese labels. It opens no link when preparing the contact fails.

## Authorization, Errors, and Logging

All use cases reuse `MaintenanceOpportunityOwnerAuthorization`. They query by `Id` and `CompanyId` directly and validate the related customer/opportunity relationship. The feature uses `OperationResult` for invalid data, missing records, forbidden access, unsupported status, and unavailable WhatsApp details. Unexpected exceptions continue to flow through the global safe exception handler.

The application does not log contact message contents or WhatsApp links. Logs may use contact, opportunity, and company identifiers only.

## Verification

Repository instructions prohibit builds, test runs, application launches, and migration execution unless the user explicitly requests them. The implementation therefore uses static verification for entity invariants, dependency-injection wiring, tenant-filtered queries, migration/snapshot consistency, endpoint contracts, and the Razor action/history states. Automated tests are not added because the project has not yet introduced its test projects.
