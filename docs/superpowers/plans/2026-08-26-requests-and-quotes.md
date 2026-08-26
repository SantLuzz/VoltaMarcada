# Service Requests and Quotes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (\`- [ ]\`) syntax for tracking.

**Goal:** Deliver a secure public request-to-quote-to-work-order flow while preserving direct internal work-order creation.

**Architecture:** A public booking submission creates an AppointmentRequest and a commercial ServiceRequest. Internal triage confirms the slot and creates either a technical visit plus draft quote or a service-execution appointment plus work order. Quotes are company-scoped aggregates with hashed public tokens; their approval atomically creates one work order carrying the approved financial snapshot.

**Tech Stack:** .NET 10, C#, ASP.NET Core Blazor Web App, EF Core, PostgreSQL, ASP.NET Core Identity, FluentValidation, MudBlazor.

**Spec:** docs/superpowers/specs/2026-08-26-requests-and-quotes-design.md

## Global Constraints

- All source code, database objects, migration names, technical documentation, comments, and identifiers are English; Brazilian Portuguese is reserved for user-facing UI text.
- Company is the tenant boundary. Authenticated operations resolve it from current-company context and never accept it from the browser.
- The public quote route is /q/{token}; raw tokens are random 32-byte values, stored only as SHA-256 hashes, and never logged.
- Use TimeProvider; do not introduce direct clock calls in Domain or Application code.
- Use explicit use cases and focused repository interfaces. Do not introduce a mediator, generic repository, unit-of-work abstraction, or new framework.
- Do not run dotnet build, dotnet test, dotnet run, migrations, or the application. The repository requires static validation only unless the user expressly requests execution.
- Do not add automated tests in this phase: repository guidance permits them only after MVP validation or when the user explicitly requests them.

---

## Planned File Structure

| Area | Files | Responsibility |
|---|---|---|
| Domain | Entities/ServiceRequest.cs, Entities/Quote.cs, Entities/QuoteItem.cs, new enums | Protect commercial state, totals, token lifecycle, and conversion invariants. |
| Application | ServiceRequests/*, Quotes/*, persistence interfaces | Authorization, commands/DTOs/use cases, validation, and errors. |
| Infrastructure | Configurations/*, Repositories/*, AppDbContext.cs, migration | Tenant persistence, indexes, FKs, and quote/work-order uniqueness. |
| Scheduling | Existing contracts, use cases, endpoints, agenda components | Appointment type and accepted-request resolution. |
| Web | Request, quote, and public quote pages | Internal triage, quote management, public decision, and WhatsApp share. |

## Task 1: Establish Domain Aggregates and Financial Snapshot Support

**Files:**

- Create: src/VoltaMarcada.Domain/Entities/ServiceRequest.cs
- Create: src/VoltaMarcada.Domain/Entities/Quote.cs
- Create: src/VoltaMarcada.Domain/Entities/QuoteItem.cs
- Create: src/VoltaMarcada.Domain/Enums/AppointmentType.cs
- Create: src/VoltaMarcada.Domain/Enums/ServiceRequestStatus.cs
- Create: src/VoltaMarcada.Domain/Enums/QuoteStatus.cs
- Create: src/VoltaMarcada.Domain/Enums/QuoteItemType.cs
- Modify: src/VoltaMarcada.Domain/Entities/Appointment.cs
- Modify: src/VoltaMarcada.Domain/Entities/AppointmentRequest.cs
- Modify: src/VoltaMarcada.Domain/Entities/WorkOrder.cs
- Modify: src/VoltaMarcada.Domain/Entities/WorkOrderItem.cs
- Modify: src/VoltaMarcada.Domain/Enums/WorkOrderItemType.cs
- Modify: src/VoltaMarcada.Domain/ValueObjects/WorkOrderItemDefinition.cs

**Interfaces:**

- Produces AppointmentType { TechnicalVisit, ServiceExecution, Other } and QuoteStatus { Draft, Sent, Approved, Rejected, Expired }.
- Produces ServiceRequest.Create(Guid companyId, Guid customerId, Guid appointmentRequestId, string? problemDescription, DateTimeOffset now).
- Produces Quote.Create(Guid companyId, Guid customerId, Guid? equipmentId, Guid primaryServiceId, Guid? serviceRequestId, IReadOnlyCollection<QuoteItemDefinition> items, decimal discountAmount, string? notes, DateOnly validUntil, DateTimeOffset now).
- Extends Appointment.Create with AppointmentType type and string? description.
- Extends WorkOrder with Guid? EquipmentId, Guid? SourceQuoteId, decimal DiscountAmount, categorized item definitions, and TotalAmount = item total - DiscountAmount.

- [ ] **Step 1: Add the enums and immutable quote-line definition.**

~~~csharp
public enum QuoteItemType { Service, Part, Material, Travel, Custom }
public enum AppointmentType { TechnicalVisit, ServiceExecution, Other }
public sealed record QuoteItemDefinition(
    string Description, decimal Quantity, decimal UnitPrice, QuoteItemType Type);
~~~

Add ServiceRequestStatus and WorkOrderItemType values. Preserve the persisted Additional numeric value and append Part, Material, and Travel.

- [ ] **Step 2: Implement protected request, quote, and quote-item state.**

~~~csharp
public void Send(string tokenHash, DateTimeOffset tokenExpiresAtUtc, DateTimeOffset now)
{
    EnsureStatus(QuoteStatus.Draft);
    PublicTokenHash = RequireTokenHash(tokenHash);
    PublicTokenExpiresAtUtc = tokenExpiresAtUtc.ToUniversalTime();
    Status = QuoteStatus.Sent;
    SentAtUtc = UpdatedAtUtc = now.ToUniversalTime();
}

public void Approve(Guid workOrderId, DateTimeOffset now)
{
    EnsureStatus(QuoteStatus.Sent);
    RequireId(workOrderId, "Work order");
    Status = QuoteStatus.Approved;
    GeneratedWorkOrderId = workOrderId;
    DecidedAtUtc = UpdatedAtUtc = now.ToUniversalTime();
}
~~~

Validate bounded descriptions, positive quantities, non-negative prices and discount, one or more service lines, date validity, and every identifier. Add behavior methods for request quote start, direct OS conversion, cancellation, and completion; never add public setters.

- [ ] **Step 3: Add appointment type, shareable description, and public problem description.**

~~~csharp
private static void EnsureTypeAssociation(AppointmentType type, Guid? workOrderId)
{
    if (type == AppointmentType.ServiceExecution && workOrderId is null)
        throw new DomainException("A service execution appointment requires a work order.");
}
~~~

Add Appointment.Type and bounded Appointment.Description without repurposing internal Notes. Add immutable, bounded AppointmentRequest.ProblemDescription. TechnicalVisit and Other may omit a work order.

- [ ] **Step 4: Extend work-order financial state.**

~~~csharp
public Guid? EquipmentId { get; private set; }
public Guid? SourceQuoteId { get; private set; }
public decimal DiscountAmount { get; private set; }
public decimal TotalAmount => _items.Sum(item => item.TotalAmount) - DiscountAmount;
~~~

Keep ServiceId as the existing primary operational service. Preserve all copied quote lines by type, including extra services, parts, materials, travel, and custom items. Allow direct OS flows to retain current Additional-item behavior. Add WorkOrder.CreateFromApprovedQuote so prices and descriptions are copied rather than read from current catalog values.

- [ ] **Step 5: Perform static Domain review.**

Run: rg -n "DateTime\.(Now|UtcNow)|public set;|QuoteStatus|AppointmentType|SourceQuoteId" src/VoltaMarcada.Domain

Expected: no direct clock calls, no new public setters, and all new values are protected by entity behavior.

- [ ] **Step 6: Commit the domain unit.**

~~~powershell
git add src/VoltaMarcada.Domain
git commit -m "Add request and quote domain models"
~~~

## Task 2: Persist Requests, Quotes, Appointment Types, and OS Snapshot Fields

**Files:**

- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/ServiceRequestConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/QuoteConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Configurations/QuoteItemConfiguration.cs
- Create: src/VoltaMarcada.Application/Abstractions/Persistence/IServiceRequestRepository.cs
- Create: src/VoltaMarcada.Application/Abstractions/Persistence/IQuoteRepository.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Repositories/ServiceRequestRepository.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Repositories/QuoteRepository.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Configurations/AppointmentConfiguration.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Configurations/AppointmentRequestConfiguration.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderConfiguration.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Configurations/WorkOrderItemConfiguration.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260826100000_AddServiceRequestsAndQuotes.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs

**Interfaces:**

- Produces service-request and quote repositories whose ID reads take both ID and company ID.
- Produces IQuoteTransaction and IQuoteRepository.BeginTransactionAsync.
- Produces a unique filtered index on WorkOrders.SourceQuoteId.

- [ ] **Step 1: Add DbSets and company-scoped configurations.**

~~~csharp
public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
public DbSet<Quote> Quotes => Set<Quote>();
public DbSet<QuoteItem> QuoteItems => Set<QuoteItem>();
~~~

Configure composite company FKs to Customer, Equipment, Service, AppointmentRequest, Quote, and WorkOrder with Restrict deletion. Configure backing collections and decimal precision (18,2).

- [ ] **Step 2: Add indexes and public-token/work-order constraints.**

~~~csharp
builder.HasIndex(x => x.PublicTokenHash).IsUnique()
    .HasFilter("\"PublicTokenHash\" IS NOT NULL");
builder.HasIndex(x => x.SourceQuoteId).IsUnique()
    .HasFilter("\"SourceQuoteId\" IS NOT NULL");
~~~

Index service requests by company/status/created time and appointment request; index quotes by company/status/created time. Persist appointment type/description, problem description, nullable OS equipment, source quote, and discount.

- [ ] **Step 3: Implement tenant-filtered repositories and transaction wrapper.**

~~~csharp
public Task<Quote?> GetForCompanyAsync(Guid quoteId, Guid companyId, CancellationToken cancellationToken) =>
    dbContext.Quotes.Include(x => x.Items).SingleOrDefaultAsync(
        x => x.Id == quoteId && x.CompanyId == companyId, cancellationToken);
~~~

The public context query accepts only token hash and filters revoked token state. Its DTO projection has company identity, customer-safe content, quote lines, and WhatsApp; it has no internal IDs.

- [ ] **Step 4: Manually author the migration and synchronize the snapshot.**

Add tables, FKs, indexes, preexisting appointment default Other, nullable equipment, and quote/work-order uniqueness. Do not run dotnet ef. Compare migration operations against every Fluent configuration and snapshot update.

- [ ] **Step 5: Perform static persistence review and commit.**

Run: rg -n "CompanyId|PublicTokenHash|SourceQuoteId|AppointmentType|DiscountAmount" src/VoltaMarcada.Infrastructure/Persistence

Expected: queries use company predicates, configurations/migration/snapshot agree, and raw tokens are absent.

~~~powershell
git add src/VoltaMarcada.Infrastructure src/VoltaMarcada.Application/Abstractions/Persistence
git commit -m "Persist service requests and quotes"
~~~

## Task 3: Turn Public Booking into a Commercial Service Request

**Files:**

- Modify: src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestCommand.cs
- Modify: src/VoltaMarcada.Application/PublicBooking/SubmitPublicAppointmentRequestUseCase.cs
- Modify: src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor

**Interfaces:**

- Extends SubmitPublicAppointmentRequestCommand with string? ProblemDescription.
- Produces one ServiceRequest linked to each submitted public AppointmentRequest.

- [ ] **Step 1: Extend the public command and validation.**

~~~csharp
public sealed record SubmitPublicAppointmentRequestCommand(
    string CompanySlug, Guid ServiceId, Guid ProfessionalId, DateOnly Date, TimeOnly StartTime,
    string CustomerName, string CustomerDocument, string CustomerWhatsApp, string? CustomerEmail,
    IReadOnlyCollection<CustomerAddressInput> Addresses, string? ProblemDescription);
~~~

Reject a value longer than 2,000 characters; retain document, rate limit, eligible service/professional, and advisory slot checks.

- [ ] **Step 2: Save appointment request and service request together.**

~~~csharp
var request = AppointmentRequest.Create(company.Id, customer.Id, service.Id, professional.Id,
    null, null, command.Date, command.StartTime, service.DurationMinutes, start, end,
    proposal, command.ProblemDescription, now);
var serviceRequest = ServiceRequest.Create(company.Id, customer.Id, request.Id, command.ProblemDescription, now);
await requestRepository.AddAsync(request, cancellationToken);
await serviceRequestRepository.AddAsync(serviceRequest, cancellationToken);
await requestRepository.SaveChangesAsync(cancellationToken);
~~~

Use the shared scoped context and do not disclose an existing customer match.

- [ ] **Step 3: Add the bounded public form field.**

~~~razor
<MudTextField @bind-Value="problemDescription" Label="Descreva o atendimento necessário"
              Lines="4" Counter="2000" MaxLength="2000" FullWidth="true" />
~~~

Pass only through the command; do not put it in the public success response or query string.

- [ ] **Step 4: Static review and commit.**

Run: rg -n "ProblemDescription|ServiceRequest.Create|SubmitPublicAppointmentRequestCommand" src/VoltaMarcada.Application/PublicBooking src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor

~~~powershell
git add src/VoltaMarcada.Application/PublicBooking src/VoltaMarcada.Web/Components/Pages/PublicCompany.razor
git commit -m "Create service requests from public booking"
~~~

## Task 4: Resolve Accepted Requests into Technical Visits, Quotes, or Direct OS

**Files:**

- Create: src/VoltaMarcada.Application/ServiceRequests/ServiceRequestContracts.cs
- Create: src/VoltaMarcada.Application/ServiceRequests/ServiceRequestErrors.cs
- Create: src/VoltaMarcada.Application/ServiceRequests/GetServiceRequestsUseCase.cs
- Create: src/VoltaMarcada.Application/ServiceRequests/ResolveServiceRequestUseCase.cs
- Modify: src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs
- Modify: src/VoltaMarcada.Application/Scheduling/GetAppointmentRequestsUseCase.cs
- Modify: src/VoltaMarcada.Application/Scheduling/SchedulingContracts.cs
- Modify: src/VoltaMarcada.Application/Scheduling/AppointmentUseCases.cs
- Modify: src/VoltaMarcada.Web/Components/Pages/Schedule.razor
- Modify: src/VoltaMarcada.Web/Endpoints/AppointmentEndpoints.cs
- Modify: src/VoltaMarcada.Web/Components/Shared/AppointmentEditor.razor

**Interfaces:**

- Produces ServiceRequestResolution { PrepareQuote, CreateWorkOrder }.
- Produces ResolveServiceRequestCommand(Guid AppointmentRequestId, ServiceRequestResolution Resolution, Guid? EquipmentId, string AppointmentDescription, decimal? ServiceUnitPrice, bool ApplyCustomerProfileProposal).
- Extends CreateAppointmentCommand with AppointmentType Type and string? Description.

- [ ] **Step 1: Replace bare acceptance with explicit resolution.**

~~~csharp
public enum ServiceRequestResolution { PrepareQuote, CreateWorkOrder }

public sealed record ResolveServiceRequestCommand(
    Guid AppointmentRequestId, ServiceRequestResolution Resolution, Guid? EquipmentId,
    string AppointmentDescription, decimal? ServiceUnitPrice, bool ApplyCustomerProfileProposal);
~~~

Load both the appointment request and linked service request using company predicates; preserve current professional authorization and profile-proposal application.

- [ ] **Step 2: Revalidate and persist the selected outcome inside the scheduling transaction.**

~~~csharp
var appointmentType = command.Resolution == ServiceRequestResolution.PrepareQuote
    ? AppointmentType.TechnicalVisit
    : AppointmentType.ServiceExecution;
~~~

Revalidate full availability, create the appointment, accept the request, then create a draft quote seeded with the selected service or create a direct work order. Seed description from ProblemDescription. A ServiceExecution appointment receives the new OS ID before entity construction; a TechnicalVisit leaves it null.

- [ ] **Step 3: Add agenda resolution UI and mandatory appointment description.**

~~~razor
<MudSelect T="ServiceRequestResolution" Label="Próximo passo" @bind-Value="resolution">
    <MudSelectItem Value="@ServiceRequestResolution.PrepareQuote">Visita técnica e orçamento</MudSelectItem>
    <MudSelectItem Value="@ServiceRequestResolution.CreateWorkOrder">Criar OS direta</MudSelectItem>
</MudSelect>
<MudTextField @bind-Value="appointmentDescription" Label="Descrição para o cliente" Required="true" />
~~~

Replace one-click confirm with this dialog. Show service, requested time, customer proposal, and submitted description. Retain suggest-time/reject operations.

- [ ] **Step 4: Make type mandatory in manual scheduling.**

Manual editor defaults to Other. ServiceExecution requires an existing OS; TechnicalVisit and Other do not. Add type/description to DTOs and agenda list/detail views.

- [ ] **Step 5: Static review and commit.**

Run: rg -n "ServiceRequestResolution|TechnicalVisit|ServiceExecution|AppointmentType|AppointmentDescription" src/VoltaMarcada.Application src/VoltaMarcada.Web

~~~powershell
git add src/VoltaMarcada.Application/ServiceRequests src/VoltaMarcada.Application/Scheduling src/VoltaMarcada.Web/Components src/VoltaMarcada.Web/Endpoints/AppointmentEndpoints.cs
git commit -m "Resolve public requests into quotes or work orders"
~~~

## Task 5: Implement Quote Lifecycle, Secure Token, and Automatic OS Creation

**Files:**

- Create: src/VoltaMarcada.Application/Quotes/QuoteContracts.cs
- Create: src/VoltaMarcada.Application/Quotes/QuoteErrors.cs
- Create: src/VoltaMarcada.Application/Quotes/QuoteCommandValidators.cs
- Create: src/VoltaMarcada.Application/Quotes/CreateQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/UpdateQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/GetQuotesUseCases.cs
- Create: src/VoltaMarcada.Application/Quotes/SendQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/ApproveQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/RejectQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/GetPublicQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/ApprovePublicQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/Quotes/RejectPublicQuoteUseCase.cs
- Modify: src/VoltaMarcada.Application/DependencyInjection.cs
- Modify: src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs

**Interfaces:**

- Produces QuoteItemInput(string Description, decimal Quantity, decimal UnitPrice, QuoteItemType Type).
- Produces CreateQuoteCommand(Guid CustomerId, Guid? EquipmentId, Guid PrimaryServiceId, Guid? ServiceRequestId, IReadOnlyList<QuoteItemInput> Items, decimal DiscountAmount, string? Notes, DateOnly ValidUntil).
- Produces QuoteApprovalResult(Guid QuoteId, Guid WorkOrderId, bool AlreadyApproved) for both internal and public approvals.

- [ ] **Step 1: Implement create/edit/list/detail commands, DTOs, validators, and errors.**

~~~csharp
var customer = await customerRepository.GetForCompanyAsync(command.CustomerId, scope.CompanyId, cancellationToken);
var service = await serviceRepository.GetForCompanyAsync(command.PrimaryServiceId, scope.CompanyId, cancellationToken);
if (customer is null || service is null || !service.IsActive)
    return OperationResult<Guid>.Failure(QuoteErrors.InvalidInput);
~~~

Require 1-100 items, one service item, valid tenant customer/equipment/service/request references, a non-negative discount, and draft-only modification. Use TimeProvider for validity checks rather than a clock in a validator.

- [ ] **Step 2: Send the quote and prepare WhatsApp URL.**

~~~csharp
var rawToken = PublicTokenGenerator.Generate();
quote.Send(PublicTokenGenerator.Hash(rawToken), expiresAtUtc, timeProvider.GetUtcNow());
var publicUrl = applicationUrlBuilder.BuildApplicationUrl($"q/{rawToken}");
whatsAppLinkBuilder.TryBuild(customer.WhatsApp,
    $"Olá, {customer.Name}! Seu orçamento está disponível em:\n{publicUrl}", out var whatsAppUrl);
~~~

Expire the token at the end of ValidUntil in UTC, revoke a replaced token, save before returning a WhatsApp URL, and never log raw URL/token.

- [ ] **Step 3: Use one transaction-safe core operation for internal and public approval.**

~~~csharp
await using var transaction = await quoteRepository.BeginTransactionAsync(cancellationToken);
var workOrder = WorkOrder.CreateFromApprovedQuote(quote, currentUser.UserId, timeProvider.GetUtcNow());
quote.Approve(workOrder.Id, timeProvider.GetUtcNow());
await workOrderRepository.AddAsync(workOrder, cancellationToken);
await quoteRepository.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
~~~

Public approval hashes its token then verifies sent, unrevoked, unexpired state before invoking this core. On unique SourceQuoteId conflict, reload quote by company and return its existing generated work order with AlreadyApproved=true.

- [ ] **Step 4: Add rejection/expiry and safe error mapping.**

Only a sent quote can be rejected. Public unavailable, invalid, revoked, expired, rejected, and decided links use one safe Brazilian Portuguese failure. Add Quote.InvalidInput, Quote.NotFound, Quote.InvalidStatus, Quote.Expired, Quote.Forbidden, and Quote.PublicLinkUnavailable mappings.

- [ ] **Step 5: Static review and commit.**

Run: rg -n "PublicTokenGenerator.Generate|PublicTokenGenerator.Hash|BeginTransactionAsync|CreateFromApprovedQuote|AlreadyApproved" src/VoltaMarcada.Application

~~~powershell
git add src/VoltaMarcada.Application/Quotes src/VoltaMarcada.Application/DependencyInjection.cs src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs
git commit -m "Add quote lifecycle and work order conversion"
~~~

## Task 6: Build Internal Request and Quote Management UI

**Files:**

- Create: src/VoltaMarcada.Web/Components/Pages/ServiceRequests.razor
- Create: src/VoltaMarcada.Web/Components/Pages/Quotes.razor
- Create: src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor
- Create: src/VoltaMarcada.Web/Components/Shared/QuoteItemsEditor.razor
- Modify: src/VoltaMarcada.Web/Components/Layout/MainLayout.razor
- Modify: src/VoltaMarcada.Web/Components/Pages/WorkOrderRegistration.razor
- Modify: src/VoltaMarcada.Web/Components/Shared/WorkOrderItemsEditor.razor

**Interfaces:**

- Consumes the Task 4 request-resolver and Task 5 quote use cases.
- Produces authenticated routes /solicitacoes, /orcamentos, and /orcamentos/registro.

- [ ] **Step 1: Add request queue, navigation, and resolution action.**

~~~razor
@page "/solicitacoes"
@attribute [Authorize(Policy = AuthorizationPolicies.CompanyMember)]
<PageHeader Title="Solicitações" Description="Avalie os pedidos de atendimento e defina o próximo passo." />
~~~

List customer, service, requested slot, problem description, and state ordered by urgency/created time. Reuse the Task 4 resolution dialog; agenda holds only a compact link to this queue.

- [ ] **Step 2: Add quote list/editor and categorized item component.**

~~~razor
<MudSelect T="QuoteItemType" Label="Tipo" @bind-Value="item.Type">
    <MudSelectItem Value="@QuoteItemType.Service">Serviço</MudSelectItem>
    <MudSelectItem Value="@QuoteItemType.Part">Peça</MudSelectItem>
    <MudSelectItem Value="@QuoteItemType.Material">Material</MudSelectItem>
    <MudSelectItem Value="@QuoteItemType.Travel">Deslocamento</MudSelectItem>
    <MudSelectItem Value="@QuoteItemType.Custom">Outro item</MudSelectItem>
</MudSelect>
~~~

The editor selects company customer, optional equipment, primary service, validity, notes, discount, and lines. Display arithmetic locally but require server recomputation. Sent/decided snapshots are read-only.

- [ ] **Step 3: Add WhatsApp, decision, and OS-origin actions.**

~~~razor
<MudButton OnClick="SendAsync" Disabled="@(quote.Status != QuoteStatus.Draft)">Enviar por WhatsApp</MudButton>
<MudButton Href="@($"/ordens-servico/registro?id={quote.GeneratedWorkOrderId}")"
           Disabled="@(quote.GeneratedWorkOrderId is null)">Abrir OS</MudButton>
~~~

Open only the URL returned after SendQuoteUseCase persists state; use existing clipboard helper when WhatsApp URL is unavailable. Show SourceQuoteId, full copied items, and discount in the OS; quote-origin snapshot stays immutable.

- [ ] **Step 4: Static review and commit.**

Run: rg -n "@page \"/solicitacoes\"|@page \"/orcamentos|QuoteItemType|Enviar por WhatsApp|SourceQuoteId" src/VoltaMarcada.Web/Components

~~~powershell
git add src/VoltaMarcada.Web/Components
git commit -m "Add request and quote management screens"
~~~

## Task 7: Deliver Secure Public Quote View, Decision, and Contact

**Files:**

- Create: src/VoltaMarcada.Web/Components/Pages/PublicQuote.razor
- Create: src/VoltaMarcada.Web/Configuration/PublicQuoteActionLimiter.cs
- Modify: src/VoltaMarcada.Web/Program.cs
- Modify: src/VoltaMarcada.Web/app.css
- Modify: src/VoltaMarcada.Application/Quotes/ApprovePublicQuoteUseCase.cs
- Modify: src/VoltaMarcada.Application/Quotes/RejectPublicQuoteUseCase.cs

**Interfaces:**

- Consumes GetPublicQuoteUseCase.ExecuteAsync(string token), public approve/reject use cases, and a limiter keyed by token hash.
- Produces /q/{Token} under PublicLayout.

- [ ] **Step 1: Add the bounded action limiter.**

~~~csharp
public sealed class PublicQuoteActionLimiter(TimeProvider timeProvider)
{
    private const int PermitLimit = 5;
    private static readonly TimeSpan Window = TimeSpan.FromMinutes(10);
    private readonly ConcurrentDictionary<string, Counter> counters = new(StringComparer.Ordinal);

    public bool TryAcquire(string tokenHash)
    {
        var now = timeProvider.GetUtcNow();
        if (counters.Count >= 10_000 && !counters.ContainsKey(tokenHash))
            foreach (var expired in counters.Where(x => now - x.Value.StartedAtUtc >= Window).Take(1_000))
                counters.TryRemove(expired.Key, out _);
        var counter = counters.GetOrAdd(tokenHash, _ => new Counter(now));
        lock (counter)
        {
            if (now - counter.StartedAtUtc >= Window) { counter.StartedAtUtc = now; counter.Count = 0; }
            if (counter.Count >= PermitLimit) return false;
            counter.Count++; return true;
        }
    }

    private sealed class Counter(DateTimeOffset startedAtUtc)
    {
        public DateTimeOffset StartedAtUtc { get; set; } = startedAtUtc;
        public int Count { get; set; }
    }
}
~~~

Implement the concrete counter behavior from PublicBookingSubmissionLimiter in this distinct class. Hash before using the token as partition key.

- [ ] **Step 2: Render only safe public quote context.**

~~~razor
@page "/q/{Token}"
@layout PublicLayout
@if (quote is null)
{
    <MudAlert Severity="Severity.Error">Este orçamento não está disponível.</MudAlert>
}
~~~

Show company identity, safe customer greeting, lines, quantities, values, discount, total, validity, notes, and current state. Do not render IDs, internal notes, request history, or raw tokens.

- [ ] **Step 3: Add approve, reject, and contact actions.**

~~~razor
<MudButton Color="Color.Success" OnClick="ApproveAsync" Disabled="@isDecided">Aprovar orçamento</MudButton>
<MudButton Color="Color.Error" Variant="Variant.Outlined" OnClick="RejectAsync" Disabled="@isDecided">Recusar</MudButton>
<MudButton Href="@whatsAppLink" Target="_blank" Variant="Variant.Text">Falar com a empresa</MudButton>
~~~

Disable duplicate actions, show a final public confirmation without an OS identifier, and surface one safe failure message for invalid/revoked/expired/decided link states.

- [ ] **Step 4: Static security review and commit.**

Run: rg -n "@page \"/q|Token|PublicQuoteActionLimiter|PublicTokenGenerator.Hash|logger\.Log" src/VoltaMarcada.Web src/VoltaMarcada.Application/Quotes

Expected: raw route token reaches only public use-case boundary; storage/query uses its hash; no raw token logging.

~~~powershell
git add src/VoltaMarcada.Web src/VoltaMarcada.Application/Quotes
git commit -m "Add public quote approval flow"
~~~

## Task 8: Reconcile Contracts and Final Static Verification

**Files:**

- Review: src/VoltaMarcada.Web/Program.cs
- Review: src/VoltaMarcada.Application/DependencyInjection.cs
- Review: src/VoltaMarcada.Infrastructure/DependencyInjection.cs
- Review: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260826100000_AddServiceRequestsAndQuotes.cs
- Review: src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs

- [ ] **Step 1: Trace all three supported paths.**

Trace direct internal customer-to-OS creation; public request to TechnicalVisit/draft quote/public approval/OS; and public request to ServiceExecution/direct OS. Confirm that every authenticated entity lookup filters by company and every ServiceExecution is created with an OS.

- [ ] **Step 2: Inspect cross-layer contract use.**

Check that endpoint/component calls match final command constructors, DTOs map all enum values, dependency registration exists, and no Razor component implements business state transitions.

- [ ] **Step 3: Run repository-wide static checks.**

~~~powershell
git diff --check
rg -n "DateTime\.(Now|UtcNow)|CompanyId.*(Request|Query|Form)" src/VoltaMarcada.Domain src/VoltaMarcada.Application
rg -n "PublicTokenHash|Generate\(\)|Hash\(" src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure src/VoltaMarcada.Web
rg -n "QuoteStatus|AppointmentType|ServiceRequestStatus" src tests
~~~

Expected: no direct clocks in Domain/Application changes; no authenticated tenant selection from browser company IDs; raw public tokens are not stored/logged; each enum is represented in entity, persistence, and presentation.

- [ ] **Step 4: Inspect schema coherence without executing migrations.**

Compare migration and snapshot to all configurations. Confirm existing appointments receive Other, existing rows tolerate nullable equipment, financial values are numeric(18,2), and public-token/source-quote unique filtered indexes match the model.

- [ ] **Step 5: Commit final integration corrections.**

~~~powershell
git add src docs
git commit -m "Finalize requests and quotes flow"
~~~

## Plan Self-Review

| Requirement | Tasks |
|---|---|
| Public input, triage, appointment purpose, no agenda dead end | 3, 4 |
| TechnicalVisit, ServiceExecution, Other | 1, 4 |
| Draft/Sent/Approved/Rejected/Expired lifecycle | 1, 5 |
| Services, parts, materials, travel, custom items, discount, notes, validity, total | 1, 2, 5, 6 |
| /q/{token}, secure view/decision/contact | 2, 5, 7 |
| WhatsApp prepared message/link | 5, 6, 7 |
| Idempotent automatic OS + financial snapshot | 1, 2, 5 |
| Direct internal OS retained | 1, 6, 8 |
| Tenant isolation, tokens, scheduling revalidation, schema | 2, 3, 4, 5, 7, 8 |

The plan contains no deferred work markers. It intentionally omits builds, application launch, migrations, and automated tests because the repository instructions prohibit them for this request.
