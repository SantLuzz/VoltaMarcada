# Phase 6 Customer Contact History Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Persist an auditable, manual WhatsApp contact history for maintenance opportunities and require explicit send confirmation before the opportunity becomes contacted.

**Architecture:** A company-scoped `CustomerContact` entity captures the immutable prepared message and its lifecycle. Application use cases enforce owner access, assemble contact context, and update the contact and opportunity atomically. Infrastructure provides the tenant-filtered persistence and WhatsApp-link construction; the existing maintenance center presents preparation, confirmation, and history.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, MudBlazor, Entity Framework Core, PostgreSQL

**Spec:** `docs/superpowers/specs/2026-08-24-phase6-customer-contact-history-design.md`

## Global Constraints

- Source code identifiers, diagnostics, logs, and technical documentation are English; UI text is Brazilian Portuguese.
- Do not add packages, WhatsApp Cloud API integration, delivery/read tracking, inbound messages, background jobs, public booking, or CRM automation.
- Every contact query and mutation resolves the company from authenticated context and filters by `CompanyId`; the browser never sends a tenant selector.
- Contacts are available only through the existing `CompanyOwner` maintenance-opportunity authorization boundary.
- Store event timestamps as UTC `DateTimeOffset`; do not use direct system clock APIs in Domain/Application logic.
- Use explicit `OperationResult` failures for expected validation, ownership, missing-data, and invalid-state outcomes.
- Do not add tests or execute builds, tests, hosts, database migrations, or `dotnet ef` commands without explicit user authorization, per `AGENTS.md`.
- Preserve unrelated working-tree changes and commit only files that belong to each task.

---

### Task 1: Add the Customer Contact Domain Entity and Database Schema

**Files:**
- Create: `src/ServiceSync.Domain/Enums/CustomerContactChannel.cs`
- Create: `src/ServiceSync.Domain/Enums/CustomerContactStatus.cs`
- Create: `src/ServiceSync.Domain/Entities/CustomerContact.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Configurations/CustomerContactConfiguration.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260824160000_AddCustomerContacts.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Interfaces:**
- Produces: immutable contact record with `Prepare` and `MarkAsSent` state behavior, `DbSet<CustomerContact> CustomerContacts`, and the `CustomerContacts` table.
- Consumed by: contact repository, maintenance opportunity query projection, contact action use cases, and the maintenance-center history UI.

- [ ] **Step 1: Define the channel and lifecycle enums**

```csharp
namespace ServiceSync.Domain.Enums;

public enum CustomerContactChannel
{
    WhatsAppLink
}

public enum CustomerContactStatus
{
    Prepared,
    Sent
}
```

Do not add future Cloud API, delivered, read, failed, or received states in this phase.

- [ ] **Step 2: Write the protected customer-contact entity**

```csharp
public sealed class CustomerContact : ICompanyScopedEntity
{
    private CustomerContact() { }

    public Guid Id { get; private set; }
    public Guid CompanyId { get; private set; }
    public Guid CustomerId { get; private set; }
    public Guid OpportunityId { get; private set; }
    public CustomerContactChannel Channel { get; private set; }
    public CustomerContactStatus Status { get; private set; }
    public string Message { get; private set; } = string.Empty;
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? SentAtUtc { get; private set; }

    public static CustomerContact Prepare(
        Guid companyId, Guid customerId, Guid opportunityId,
        CustomerContactChannel channel, string message, DateTimeOffset now);
    public void MarkAsSent(DateTimeOffset now);
}
```

Require non-empty IDs and a trimmed message no longer than 2,000 characters. `Prepare` stores a UTC timestamp and `Prepared` status. `MarkAsSent` only permits `Prepared`; it writes UTC `SentAtUtc` and changes status to `Sent`, otherwise throws `DomainException`.

- [ ] **Step 3: Map tenant-scoped relationships and indexes**

```csharp
builder.ToTable("CustomerContacts");
builder.HasKey(x => x.Id);
builder.Property(x => x.Id).ValueGeneratedNever();
builder.Property(x => x.Message).HasMaxLength(2000).IsRequired();
builder.HasIndex(x => new { x.CompanyId, x.OpportunityId, x.CreatedAtUtc })
    .HasDatabaseName("IX_CustomerContacts_CompanyId_OpportunityId_CreatedAtUtc");
builder.HasIndex(x => new { x.CompanyId, x.CustomerId, x.CreatedAtUtc })
    .HasDatabaseName("IX_CustomerContacts_CompanyId_CustomerId_CreatedAtUtc");
```

Map restrictive foreign keys to `Company`, `Customer` through `(CompanyId, Id)`, and `MaintenanceOpportunity` through its existing `(CompanyId, Id)` alternate key. Add the `DbSet` to `AppDbContext`.

- [ ] **Step 4: Write the reversible manual migration and matching snapshot**

Create `CustomerContacts` with UUID identifiers, `varchar(2000)` message, integer enum columns, UTC timestamp columns, the three restrictive foreign keys, and the two named indexes. `Down` drops only `CustomerContacts`. Add the identical entity properties, indexes, and relationships to `AppDbContextModelSnapshot`.

- [ ] **Step 5: Perform static schema verification**

Run: `rg -n "CustomerContact|CustomerContacts|IX_CustomerContacts|public set;|DateTime\.(Now|UtcNow)" src/ServiceSync.Domain src/ServiceSync.Infrastructure/Persistence`

Expected: the new entity has private setters, no direct domain/application clock calls, and configuration, migration, and snapshot use the same table/index names.

- [ ] **Step 6: Commit the aggregate and schema**

```bash
git add src/ServiceSync.Domain/Enums/CustomerContactChannel.cs src/ServiceSync.Domain/Enums/CustomerContactStatus.cs src/ServiceSync.Domain/Entities/CustomerContact.cs src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs src/ServiceSync.Infrastructure/Persistence/Configurations/CustomerContactConfiguration.cs src/ServiceSync.Infrastructure/Persistence/Migrations/20260824160000_AddCustomerContacts.cs src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
git commit -m "feat: add customer contact history"
```

### Task 2: Add Tenant-Scoped Contact Persistence and WhatsApp Link Construction

**Files:**
- Create: `src/ServiceSync.Application/Abstractions/Persistence/ICustomerContactRepository.cs`
- Create: `src/ServiceSync.Application/Abstractions/Communication/IWhatsAppLinkBuilder.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Repositories/CustomerContactRepository.cs`
- Create: `src/ServiceSync.Infrastructure/Communication/WhatsAppLinkBuilder.cs`
- Modify: `src/ServiceSync.Infrastructure/DependencyInjection.cs`
- Modify: `src/ServiceSync.Application/Abstractions/Persistence/IMaintenanceOpportunityRepository.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs`
- Modify: `src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs`

**Interfaces:**
- Produces: `ICustomerContactRepository`, `IWhatsAppLinkBuilder`, contact context, and history DTOs returned with every maintenance-opportunity item.
- Consumed by: contact preparation/confirmation use cases and `/manutencoes`.

- [ ] **Step 1: Define the contact repository and communication contracts**

```csharp
public interface ICustomerContactRepository
{
    Task AddAsync(CustomerContact contact, CancellationToken cancellationToken = default);
    Task<CustomerContact?> GetForCompanyAsync(
        Guid contactId, Guid companyId, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface IWhatsAppLinkBuilder
{
    bool TryBuild(string phoneNumber, string message, out string link);
}
```

`TryBuild` must return `false` rather than throw for blank/non-numeric phone data. It normalizes the phone to digits and URL-encodes the message in the Infrastructure implementation. No Razor component formats a WhatsApp URL.

- [ ] **Step 2: Add contact-context and history contracts**

```csharp
public sealed record MaintenanceOpportunityContactContext(
    Guid OpportunityId, Guid CustomerId, string CustomerName, string CustomerWhatsApp,
    string EquipmentDescription, string RecommendedServiceName, DateOnly DueDate,
    MaintenanceOpportunityStatus Status);

public sealed record CustomerContactHistoryItem(
    Guid Id, CustomerContactChannel Channel, CustomerContactStatus Status,
    string Message, DateTimeOffset CreatedAtUtc, DateTimeOffset? SentAtUtc);

public sealed record PreparedWhatsAppContact(Guid ContactId, string Link);
```

Extend `MaintenanceOpportunityListItem` with `IReadOnlyList<CustomerContactHistoryItem> Contacts`. Keep its existing scalar fields unchanged so the maintenance center preserves all previous display behavior.

- [ ] **Step 3: Implement tenant-filtered repository queries**

Add `GetContactContextAsync(Guid opportunityId, Guid companyId, CancellationToken)` to `IMaintenanceOpportunityRepository`; it joins only company-filtered opportunity, customer, equipment, and service data. Return no result if any related record is outside the tenant.

In `GetListAsync`, fetch contacts in a second company-filtered query for the selected opportunity IDs, group them by `OpportunityId`, order each group by `CreatedAtUtc` descending, and attach the projected `CustomerContactHistoryItem` collection to each result. Do not issue one database query per list row.

- [ ] **Step 4: Register implementations and verify query isolation**

Register `ICustomerContactRepository` and `IWhatsAppLinkBuilder` as scoped services in Infrastructure. Preserve the shared scoped `AppDbContext` so one `SaveChangesAsync` can persist the contact and opportunity mutations together.

Run: `rg -n "GetContactContextAsync|CustomerContacts|CompanyId == companyId|wa\.me|Uri\.EscapeDataString" src/ServiceSync.Application src/ServiceSync.Infrastructure`

Expected: every contact query has a company predicate and WhatsApp URL formatting appears only in `WhatsAppLinkBuilder`.

- [ ] **Step 5: Commit persistence and communication infrastructure**

```bash
git add src/ServiceSync.Application/Abstractions/Persistence/ICustomerContactRepository.cs src/ServiceSync.Application/Abstractions/Communication/IWhatsAppLinkBuilder.cs src/ServiceSync.Application/Abstractions/Persistence/IMaintenanceOpportunityRepository.cs src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs src/ServiceSync.Infrastructure/Persistence/Repositories/CustomerContactRepository.cs src/ServiceSync.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs src/ServiceSync.Infrastructure/Communication/WhatsAppLinkBuilder.cs src/ServiceSync.Infrastructure/DependencyInjection.cs
git commit -m "feat: add customer contact persistence"
```

### Task 3: Implement Explicit Contact Preparation and Sending Use Cases

**Files:**
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/CustomerContactErrors.cs`
- Create: `src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityContactUseCases.cs`
- Modify: `src/ServiceSync.Application/DependencyInjection.cs`
- Modify: `src/ServiceSync.Web/Errors/OperationErrorMessages.cs`
- Modify: `src/ServiceSync.Web/Endpoints/MaintenanceOpportunityEndpoints.cs`

**Interfaces:**
- Produces: `PrepareMaintenanceOpportunityWhatsAppContactUseCase` and `ConfirmMaintenanceOpportunityContactSentUseCase`, plus owner endpoints that return the prepared link and accept a contact ID for confirmation.
- Consumed by: the maintenance-center WhatsApp and confirmation actions.

- [ ] **Step 1: Define expected contact failures**

```csharp
public static class CustomerContactErrors
{
    public static readonly OperationError InvalidInput = new(
        "CustomerContact.InvalidInput", OperationErrorType.Validation,
        "Customer contact input is invalid.");
    public static readonly OperationError NotFound = new(
        "CustomerContact.NotFound", OperationErrorType.NotFound,
        "Customer contact was not found.");
    public static readonly OperationError InvalidStatus = new(
        "CustomerContact.InvalidStatus", OperationErrorType.Conflict,
        "Customer contact status does not allow this action.");
    public static readonly OperationError WhatsAppUnavailable = new(
        "CustomerContact.WhatsAppUnavailable", OperationErrorType.Validation,
        "Customer WhatsApp number is unavailable.");
}
```

Add safe Portuguese messages to `OperationErrorMessages`; never return a raw phone number or message in an error response.

- [ ] **Step 2: Prepare the record and link before opening WhatsApp**

```csharp
public sealed class PrepareMaintenanceOpportunityWhatsAppContactUseCase(
    IMaintenanceOpportunityRepository opportunityRepository,
    ICustomerContactRepository contactRepository,
    IWhatsAppLinkBuilder whatsAppLinkBuilder,
    MaintenanceOpportunityOwnerAuthorization authorization,
    TimeProvider timeProvider)
{
    public Task<OperationResult<PreparedWhatsAppContact>> ExecuteAsync(
        Guid opportunityId, CancellationToken cancellationToken = default);
}
```

Resolve owner company access first. Load the tenant-filtered contact context, then require the opportunity state to be `Pending` or `Contacted`. Generate one Portuguese message from customer name, equipment description, recommended service, and due date context. Build the link before adding the entity; return `WhatsAppUnavailable` if it cannot be built. Then create `CustomerContact.Prepare(...)`, save it, and return its ID and link.

- [ ] **Step 3: Confirm sending and update the commercial state atomically**

```csharp
public sealed class ConfirmMaintenanceOpportunityContactSentUseCase(
    ICustomerContactRepository contactRepository,
    IMaintenanceOpportunityRepository opportunityRepository,
    MaintenanceOpportunityOwnerAuthorization authorization,
    TimeProvider timeProvider)
{
    public Task<OperationResult> ExecuteAsync(
        Guid opportunityId, Guid contactId, CancellationToken cancellationToken = default);
}
```

Require non-empty identifiers and owner access. Load both aggregates using the resolved `CompanyId`; reject a contact whose `OpportunityId` differs. Require `Prepared` and the opportunity state `Pending` or `Contacted`. Call `contact.MarkAsSent(now)` then `opportunity.MarkAsContacted(now)` and invoke one repository `SaveChangesAsync`. Invalid state returns `CustomerContact.InvalidStatus`; unexpected exceptions remain for the global handler.

- [ ] **Step 4: Add owner-only endpoint contracts**

```csharp
group.MapPost("/{opportunityId:guid}/contacts/whatsapp", async (
    Guid opportunityId,
    PrepareMaintenanceOpportunityWhatsAppContactUseCase useCase,
    CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, cancellationToken))
        .ToHttpResult(value => Results.Ok(value)));

group.MapPost("/{opportunityId:guid}/contacts/{contactId:guid}/sent", async (
    Guid opportunityId,
    Guid contactId,
    ConfirmMaintenanceOpportunityContactSentUseCase useCase,
    CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, contactId, cancellationToken))
        .ToHttpResult(_ => Results.Ok()));
```

Keep these routes inside the existing owner-only maintenance group. Remove the old untracked `/contact` route once the UI is migrated.

- [ ] **Step 5: Register and statically verify the workflow**

Register both use cases as scoped services.

Run: `rg -n "PrepareMaintenanceOpportunityWhatsAppContactUseCase|ConfirmMaintenanceOpportunityContactSentUseCase|MarkAsSent|MarkAsContacted|contacts/whatsapp|contacts/.*/sent" src/ServiceSync.Application src/ServiceSync.Web`

Expected: preparation persists before link return; confirmation is the only new path that changes an opportunity to contacted; both endpoints require owner authorization through their route group.

- [ ] **Step 6: Commit the contact workflow**

```bash
git add src/ServiceSync.Application/MaintenanceOpportunities/CustomerContactErrors.cs src/ServiceSync.Application/MaintenanceOpportunities/MaintenanceOpportunityContactUseCases.cs src/ServiceSync.Application/DependencyInjection.cs src/ServiceSync.Web/Errors/OperationErrorMessages.cs src/ServiceSync.Web/Endpoints/MaintenanceOpportunityEndpoints.cs
git commit -m "feat: track maintenance WhatsApp contacts"
```

### Task 4: Replace the Maintenance-Center Contact Action with History and Confirmation

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/MaintenanceOpportunities.razor`

**Interfaces:**
- Consumes: `MaintenanceOpportunityListItem.Contacts`, `PrepareMaintenanceOpportunityWhatsAppContactUseCase`, and `ConfirmMaintenanceOpportunityContactSentUseCase`.
- Produces: assisted WhatsApp preparation, explicit send confirmation, and visible per-opportunity history.

- [ ] **Step 1: Inject the new use cases and remove the bypass action**

Replace `MarkMaintenanceOpportunityAsContactedUseCase` injection and its `ContactAsync` helper with the preparation and confirmation use cases. Remove the visible **Marcar contato** button so a WhatsApp contact cannot transition without a saved contact record.

- [ ] **Step 2: Prepare a link, refresh state, then open it**

```csharp
private async Task PrepareWhatsAppAsync(Guid opportunityId)
{
    var result = await PrepareWhatsAppContactUseCase.ExecuteAsync(opportunityId);
    if (!result.IsSuccess)
    {
        Snackbar.Add(OperationErrorMessages.GetMessage(result.Error?.Code), Severity.Warning);
        return;
    }

    await LoadAsync();
    await JsRuntime.InvokeVoidAsync("open", result.Value!.Link, "_blank");
}
```

Use the existing item action loading state to prevent duplicate preparation. The component does not construct messages, normalize phone numbers, or encode URLs.

- [ ] **Step 3: Render the contact timeline and explicit send confirmation**

Add a MudBlazor expansion panel named **Historico de contatos** below the opportunity’s action row. For every `item.Contacts` entry, display the channel label, prepared/sent status, creation timestamp, sent timestamp when present, and the immutable message. For every `Prepared` entry, show **Confirmar envio**, invoking `ConfirmMaintenanceOpportunityContactSentUseCase.ExecuteAsync(item.Id, contact.Id)`. Refresh list and summary on success.

Only render the WhatsApp preparation button for `Pending` or `Contacted`. Keep schedule, complete, and dismissal conditions exactly as in Phase 5.

- [ ] **Step 4: Verify the component’s allowed paths**

Run: `rg -n "PrepareWhatsAppAsync|Confirmar envio|Historico de contatos|CustomerContactStatus\.Prepared|MarkContactedUseCase|wa\.me|EscapeDataString" src/ServiceSync.Web/Components/Pages/MaintenanceOpportunities.razor`

Expected: history and confirmation exist; the old direct contact action and client-side WhatsApp URL construction are absent.

- [ ] **Step 5: Commit the UI migration**

```bash
git add src/ServiceSync.Web/Components/Pages/MaintenanceOpportunities.razor
git commit -m "feat: show maintenance contact history"
```

### Task 5: Review the Complete Phase Statically

**Files:**
- Verify: all files listed in Tasks 1–4

**Interfaces:**
- Verifies: approved Phase 6 behavior, tenant isolation, state transitions, schema consistency, and the absence of unapproved WhatsApp automation.

- [ ] **Step 1: Run repository consistency checks**

Run: `git diff --check`

Expected: no whitespace errors.

Run: `rg -n "CustomerContact|WhatsAppCloudApi|wa\.me|IWhatsAppLinkBuilder|CompanyId == companyId" src/ServiceSync.Domain src/ServiceSync.Application src/ServiceSync.Infrastructure src/ServiceSync.Web`

Expected: `WhatsAppCloudApi` appears only as future-facing documentation if at all; the link builder is the sole URL construction location; repository paths query contacts by company.

- [ ] **Step 2: Inspect all changes against the approved specification**

Confirm manually that:

- opening WhatsApp creates a `Prepared` contact with the exact saved message;
- confirmation changes only that contact to `Sent`, writes `SentAtUtc`, and marks the matching opportunity as contacted;
- no link opens after an expected preparation failure;
- contact history is shown per opportunity in descending time order;
- no contact route or component accepts `CompanyId` from the browser;
- migration, entity configuration, and model snapshot agree on all columns, keys, and indexes.

Do not run build, tests, application host, migration, or EF tooling without new user authorization.
