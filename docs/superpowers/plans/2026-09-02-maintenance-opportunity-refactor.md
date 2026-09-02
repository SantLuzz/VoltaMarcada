# Maintenance Opportunity Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox ("- [ ]") syntax for tracking.

**Goal:** Make maintenance records track return-customer opportunities that automatically close when converted to an appointment or quote, or when discarded with a recorded reason.

**Architecture:** Extend MaintenanceOpportunity with opportunity-oriented states and minimal conversion/closure metadata. Existing appointment, quote, contact, and public-booking use cases remain the only workflows that create their own entities; they call maintenance behavior atomically after a successful creation. The UI projects those references into compact cards while putting history and secondary actions in details.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core, PostgreSQL, MudBlazor, xUnit.

**Spec:** docs/superpowers/specs/2026-09-02-maintenance-opportunities-design.md

## Global Constraints

- Preserve tenant isolation: resolve company from authenticated context and filter all tenant data by CompanyId.
- Do not trust browser-supplied company, customer, equipment, service, or opportunity IDs for conversion authorization.
- Keep source code, migrations, tests, logs, and technical documentation in English; Brazilian Portuguese is only for user-facing UI text.
- Use TimeProvider; do not add direct clock calls to domain or application logic.
- Use explicit application use cases and OperationResult for expected business failures.
- Reuse Agenda and Quote Registration; do not create a maintenance-specific scheduling or quote editor.
- Use MudBlazor controls, dialogs, menus, chips, grids, and expansion panels for the modified UI.
- Do not stage, overwrite, or discard the existing unrelated local changes in AppointmentUseCases.cs, QuoteRegistration.razor, or the pending appointment test; fold the relevant appointment change into the final behavior.

---

## File Structure

- src/VoltaMarcada.Domain/Enums/MaintenanceOpportunityStatus.cs -- terminal/open opportunity states with compatible numeric values.
- src/VoltaMarcada.Domain/Enums/MaintenanceOpportunityConversionType.cs -- type of newly created converted entity.
- src/VoltaMarcada.Domain/Entities/MaintenanceOpportunity.cs -- state-transition rules and conversion/closure audit data.
- src/VoltaMarcada.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs -- column limits and query indexes.
- src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260902100000_RefactorMaintenanceOpportunityLifecycle.cs and model snapshot -- persistence upgrade and legacy status conversion.
- src/VoltaMarcada.Application/MaintenanceOpportunities -- conversion context, list/detail DTOs, discard command, and query projections.
- src/VoltaMarcada.Application/Scheduling and src/VoltaMarcada.Application/PublicBooking -- appointment conversion at each actual appointment-creation boundary.
- src/VoltaMarcada.Application/Quotes -- quote conversion context and atomic creation.
- src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor -- compact opportunity screen and detail UI.
- tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunity*.cs -- lifecycle, conversion, and migration coverage.

### Task 1: Establish the opportunity lifecycle and database compatibility

**Files:**
- Create: src/VoltaMarcada.Domain/Enums/MaintenanceOpportunityConversionType.cs
- Create: tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityLifecycleTests.cs
- Create: src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260902100000_RefactorMaintenanceOpportunityLifecycle.cs
- Modify: src/VoltaMarcada.Domain/Enums/MaintenanceOpportunityStatus.cs
- Modify: src/VoltaMarcada.Domain/Entities/MaintenanceOpportunity.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs
- Modify: tests/VoltaMarcada.IntegrationTests/PhaseSevenMigrationTests.cs

**Interfaces:**
- Produces MaintenanceOpportunityStatus.Pending, Contacted, AwaitingCustomer, Converted, and ClosedWithoutConversion.
- Produces MaintenanceOpportunityConversionType.Appointment and Quote.
- Produces MarkAsAwaitingCustomer(DateTimeOffset now), Convert(MaintenanceOpportunityConversionType type, Guid entityId, DateTimeOffset now), and CloseWithoutConversion(Guid closedByUserId, string reason, DateTimeOffset now).
- Produces nullable ConversionType, ConvertedEntityId, ConvertedAtUtc, ClosedAtUtc, ClosedByUserId, and ClosureReason properties.

- [ ] **Step 1: Write the failing lifecycle tests**

~~~csharp
[Fact]
public void Convert_WhenOpen_RecordsTheEntityReferenceAndMakesTheOpportunityTerminal()
{
    var opportunity = CreateOpportunity();
    var appointmentId = Guid.NewGuid();

    opportunity.Convert(MaintenanceOpportunityConversionType.Appointment, appointmentId, Now);

    Assert.Equal(MaintenanceOpportunityStatus.Converted, opportunity.Status);
    Assert.Equal(MaintenanceOpportunityConversionType.Appointment, opportunity.ConversionType);
    Assert.Equal(appointmentId, opportunity.ConvertedEntityId);
    Assert.Equal(Now, opportunity.ConvertedAtUtc);
    Assert.Throws<DomainException>(() => opportunity.MarkAsAwaitingCustomer(Now));
}

[Fact]
public void CloseWithoutConversion_WhenReasonIsBlank_Throws()
{
    Assert.Throws<DomainException>(() =>
        CreateOpportunity().CloseWithoutConversion(Guid.NewGuid(), " ", Now));
}
~~~

- [ ] **Step 2: Run the test to verify it fails**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunityLifecycleTests

Expected: FAIL because the conversion type and lifecycle methods do not exist.

- [ ] **Step 3: Write the minimal lifecycle implementation**

~~~csharp
public enum MaintenanceOpportunityStatus
{
    Pending = 0,
    Contacted = 1,
    AwaitingCustomer = 2,
    Converted = 3,
    ClosedWithoutConversion = 5
}

public void Convert(MaintenanceOpportunityConversionType type, Guid entityId, DateTimeOffset now)
{
    EnsureOpen();
    if (!Enum.IsDefined(type) || entityId == Guid.Empty)
        throw new DomainException("Maintenance opportunity conversion is invalid.");

    var utcNow = now.ToUniversalTime();
    Status = MaintenanceOpportunityStatus.Converted;
    ConversionType = type;
    ConvertedEntityId = entityId;
    ConvertedAtUtc = UpdatedAtUtc = utcNow;
}
~~~

Implement CloseWithoutConversion with a non-empty user ID and trimmed reason (maximum 2000 characters). Let SetBookingToken transition to AwaitingCustomer. Let RevokeBookingToken change AwaitingCustomer back to Contacted. Remove the obsolete completed/dismissed transition methods.

- [ ] **Step 4: Add configuration and migration**

Create the migration:

~~~powershell
dotnet ef migrations add RefactorMaintenanceOpportunityLifecycle --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --output-dir Persistence/Migrations
~~~

Use migration name/timestamp 20260902100000_RefactorMaintenanceOpportunityLifecycle. Add the six nullable audit columns, ClosureReason maximum length 2000, and this compatibility conversion:

~~~csharp
migrationBuilder.Sql("""
    UPDATE "MaintenanceOpportunities"
    SET "Status" = 3,
        "ConvertedAtUtc" = COALESCE("CompletedAtUtc", "BookedAtUtc", "UpdatedAtUtc")
    WHERE "Status" = 4;
    """);
~~~

Status 2, 3, and 5 retain their persisted numeric values and therefore become AwaitingCustomer, Converted, and ClosedWithoutConversion. Do not invent conversion references for historical converted rows. Do not duplicate the existing CompanyId/Status/DueDate index.

- [ ] **Step 5: Run lifecycle and migration-catalog tests**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~MaintenanceOpportunityLifecycleTests|FullyQualifiedName~PhaseSevenMigrationTests"

Expected: PASS. Add 20260902100000_RefactorMaintenanceOpportunityLifecycle to the migration-catalog assertion.

- [ ] **Step 6: Commit**

~~~powershell
git add src/VoltaMarcada.Domain/Enums/MaintenanceOpportunityStatus.cs src/VoltaMarcada.Domain/Enums/MaintenanceOpportunityConversionType.cs src/VoltaMarcada.Domain/Entities/MaintenanceOpportunity.cs src/VoltaMarcada.Infrastructure/Persistence/Configurations/MaintenanceOpportunityConfiguration.cs src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260902100000_RefactorMaintenanceOpportunityLifecycle.cs src/VoltaMarcada.Infrastructure/Persistence/Migrations/20260902100000_RefactorMaintenanceOpportunityLifecycle.Designer.cs src/VoltaMarcada.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityLifecycleTests.cs tests/VoltaMarcada.IntegrationTests/PhaseSevenMigrationTests.cs
git commit -m "Refactor maintenance opportunity lifecycle"
~~~

### Task 2: Convert opportunities from every appointment creation path

**Files:**
- Modify: src/VoltaMarcada.Domain/Entities/Appointment.cs
- Modify: src/VoltaMarcada.Application/Scheduling/AppointmentUseCases.cs
- Modify: src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs
- Modify: src/VoltaMarcada.Application/PublicBooking/AppointmentRequestResponseUseCases.cs
- Modify: src/VoltaMarcada.Application/PublicBooking/SubmitContextualAppointmentRequestUseCase.cs
- Modify: src/VoltaMarcada.Application/PublicBooking/GetContextualMaintenanceBookingUseCase.cs
- Modify: src/VoltaMarcada.Application/PublicBooking/GetPublicAvailableSlotsUseCase.cs
- Modify: tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityAppointmentTests.cs
- Create: tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityPublicBookingTests.cs

**Interfaces:**
- Consumes MaintenanceOpportunity.Convert from Task 1.
- Produces appointment creation that preserves Service, Visit, or Other when MaintenanceOpportunityId is set.
- Produces public-context checks against MaintenanceOpportunityStatus.AwaitingCustomer.

- [ ] **Step 1: Write failing appointment conversion tests**

~~~csharp
[Theory]
[InlineData(AppointmentType.Service)]
[InlineData(AppointmentType.Visit)]
[InlineData(AppointmentType.Other)]
public async Task CreateAsync_FromMaintenanceOpportunity_PreservesTypeAndConverts(AppointmentType type)
{
    var result = await useCase.ExecuteAsync(CreateCommand(opportunity.Id, type));

    Assert.True(result.IsSuccess);
    Assert.Equal(type, appointments.AddedAppointment!.Type);
    Assert.Equal(MaintenanceOpportunityStatus.Converted, opportunity.Status);
    Assert.Equal(MaintenanceOpportunityConversionType.Appointment, opportunity.ConversionType);
    Assert.Equal(result.Value, opportunity.ConvertedEntityId);
}
~~~

Add a public booking acceptance test with an AwaitingCustomer opportunity and assert it stores the appointment ID after an accepted request.

- [ ] **Step 2: Run the test to verify it fails**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~MaintenanceOpportunityAppointmentTests|FullyQualifiedName~MaintenanceOpportunityPublicBookingTests"

Expected: FAIL because the current code rewrites the type to Visit and moves the opportunity to the old booked status.

- [ ] **Step 3: Write the minimal appointment conversion implementation**

Remove the service-appointment requirement for a work order in Appointment.Create; retain the rule that a work order cannot be associated with Visit or Other. In CreateAppointmentUseCase resolve customer/equipment/service from the tenant-scoped source opportunity, but do not rewrite Type. Before TrySaveAppointmentChangesAsync, convert the entity in the existing scheduling transaction:

~~~csharp
maintenanceOpportunity?.Convert(
    MaintenanceOpportunityConversionType.Appointment,
    appointment.Id,
    timeProvider.GetUtcNow());
~~~

Apply the same conversion behavior in AcceptAppointmentRequestUseCase and RespondToAppointmentRequestUseCase. A missing/stale source opportunity must return an existing invalid-status result rather than accepting an untracked request. Replace BookingRequested checks with AwaitingCustomer in all contextual-public-booking use cases. Submitting a public preference must not convert; only the successful appointment creation converts.

- [ ] **Step 4: Run the test to verify it passes**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~MaintenanceOpportunityAppointmentTests|FullyQualifiedName~MaintenanceOpportunityPublicBookingTests"

Expected: PASS. Edit the existing pending appointment test rather than replacing it.

- [ ] **Step 5: Commit**

~~~powershell
git add src/VoltaMarcada.Domain/Entities/Appointment.cs src/VoltaMarcada.Application/Scheduling/AppointmentUseCases.cs src/VoltaMarcada.Application/Scheduling/AcceptAppointmentRequestUseCase.cs src/VoltaMarcada.Application/PublicBooking/AppointmentRequestResponseUseCases.cs src/VoltaMarcada.Application/PublicBooking/SubmitContextualAppointmentRequestUseCase.cs src/VoltaMarcada.Application/PublicBooking/GetContextualMaintenanceBookingUseCase.cs src/VoltaMarcada.Application/PublicBooking/GetPublicAvailableSlotsUseCase.cs tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityAppointmentTests.cs tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityPublicBookingTests.cs
git commit -m "Convert maintenance opportunities to appointments"
~~~

### Task 3: Reuse Quote Registration and convert the source opportunity

**Files:**
- Modify: src/VoltaMarcada.Application/Quotes/QuoteContracts.cs
- Modify: src/VoltaMarcada.Application/Quotes/CreateQuoteUseCase.cs
- Create: src/VoltaMarcada.Application/MaintenanceOpportunities/GetMaintenanceOpportunityForQuoteUseCase.cs
- Modify: src/VoltaMarcada.Application/DependencyInjection.cs
- Modify: src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor
- Create: tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityQuoteTests.cs

**Interfaces:**
- Produces MaintenanceOpportunityQuoteContext(Guid OpportunityId, Guid CustomerId, Guid EquipmentId, Guid RecommendedServiceId).
- Extends CreateQuoteCommand with Guid? MaintenanceOpportunityId = null after SourceAppointmentId.
- Produces GetMaintenanceOpportunityForQuoteUseCase.ExecuteAsync(Guid opportunityId, CancellationToken).

- [ ] **Step 1: Write failing quote conversion tests**

~~~csharp
[Fact]
public async Task CreateAsync_FromMaintenanceOpportunity_UsesTrustedContextAndRecordsQuoteConversion()
{
    var result = await useCase.ExecuteAsync(
        CreateCommandWithDifferentBrowserIds() with { MaintenanceOpportunityId = opportunity.Id });

    Assert.True(result.IsSuccess);
    Assert.Equal(opportunity.CustomerId, quotes.AddedQuote!.CustomerId);
    Assert.Equal(opportunity.EquipmentId, quotes.AddedQuote.EquipmentId);
    Assert.Equal(opportunity.RecommendedServiceId, quotes.AddedQuote.PrimaryServiceId);
    Assert.Equal(MaintenanceOpportunityConversionType.Quote, opportunity.ConversionType);
    Assert.Equal(result.Value, opportunity.ConvertedEntityId);
}
~~~

Also write a test that a closed opportunity creates no quote.

- [ ] **Step 2: Run the test to verify it fails**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunityQuoteTests

Expected: FAIL because quote contracts and use cases have no maintenance source.

- [ ] **Step 3: Write the minimal quote conversion implementation**

Inject IMaintenanceOpportunityRepository into CreateQuoteUseCase. Load the open source by scope.CompanyId, replace command customer/equipment/service IDs with entity values, then validate and save atomically:

~~~csharp
await using var transaction = await quotes.BeginTransactionAsync(cancellationToken);
await quotes.AddAsync(quote, cancellationToken);
opportunity?.Convert(MaintenanceOpportunityConversionType.Quote, quote.Id, timeProvider.GetUtcNow());
await quotes.SaveChangesAsync(cancellationToken);
await transaction.CommitAsync(cancellationToken);
~~~

GetMaintenanceOpportunityForQuoteUseCase must require owner authorization and return only an open, tenant-scoped context. QuoteRegistration receives maintenanceOpportunityId, resolves it at initialization, pre-fills/locks only those source selectors, and passes it to CreateQuoteCommand for a new quote. Preserve the existing local Brazilian currency-input and PDF-link changes.

- [ ] **Step 4: Run the test to verify it passes**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunityQuoteTests

Expected: PASS with trusted prefill, atomic conversion, and no quote from a closed opportunity.

- [ ] **Step 5: Commit**

~~~powershell
git add src/VoltaMarcada.Application/Quotes/QuoteContracts.cs src/VoltaMarcada.Application/Quotes/CreateQuoteUseCase.cs src/VoltaMarcada.Application/MaintenanceOpportunities/GetMaintenanceOpportunityForQuoteUseCase.cs src/VoltaMarcada.Application/DependencyInjection.cs src/VoltaMarcada.Web/Components/Pages/QuoteRegistration.razor tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityQuoteTests.cs
git commit -m "Convert maintenance opportunities to quotes"
~~~

### Task 4: Project traceability and implement audited discard

**Files:**
- Modify: src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityDtos.cs
- Modify: src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityActionUseCases.cs
- Modify: src/VoltaMarcada.Application/MaintenanceOpportunities/MaintenanceOpportunityContactUseCases.cs
- Modify: src/VoltaMarcada.Application/MaintenanceOpportunities/GetMaintenanceOpportunitySummaryUseCase.cs
- Modify: src/VoltaMarcada.Application/Abstractions/Persistence/IMaintenanceOpportunityRepository.cs
- Modify: src/VoltaMarcada.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs
- Modify: src/VoltaMarcada.Web/Endpoints/MaintenanceOpportunityEndpoints.cs
- Modify: src/VoltaMarcada.Application/DependencyInjection.cs
- Modify: tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityLifecycleTests.cs

**Interfaces:**
- Produces DiscardMaintenanceOpportunityCommand(string Reason) and DiscardMaintenanceOpportunityUseCase.ExecuteAsync(Guid, DiscardMaintenanceOpportunityCommand, CancellationToken).
- Produces list/detail data containing conversion metadata, closure metadata, full contact history, and AwaitingCustomerCount.
- Removes MarkMaintenanceOpportunityAsBookedUseCase, CompleteMaintenanceOpportunityUseCase, and obsolete endpoint routes.

- [ ] **Step 1: Write failing discard/projection tests**

~~~csharp
[Fact]
public async Task DiscardAsync_RecordsCurrentUserAndMandatoryReason()
{
    var result = await useCase.ExecuteAsync(
        opportunity.Id,
        new DiscardMaintenanceOpportunityCommand("Customer declined"));

    Assert.True(result.IsSuccess);
    Assert.Equal(currentUser.UserId, opportunity.ClosedByUserId);
    Assert.Equal("Customer declined", opportunity.ClosureReason);
}

[Fact]
public async Task GetListAsync_ProjectsQuoteReferenceWithoutCopyingQuoteData()
{
    var item = Assert.Single(await repository.GetListAsync(companyId, new(null, null), Today));

    Assert.Equal(MaintenanceOpportunityConversionType.Quote, item.ConversionType);
    Assert.Equal(quote.Id, item.ConvertedEntityId);
    Assert.Equal(QuoteStatus.Sent, item.ConversionReference!.QuoteStatus);
}
~~~

- [ ] **Step 2: Run the test to verify it fails**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunityLifecycleTests

Expected: FAIL because discard has no reason/current-user audit and projection has no conversion reference.

- [ ] **Step 3: Write the minimal action/query implementation**

Replace generic old booked/completed/dismissed actions with focused discard behavior. It must obtain ICurrentUser.UserId, owner company ID, and the tenant-filtered record before CloseWithoutConversion. Contact prepare/confirm stays on CustomerContact but uses AwaitingCustomer and rejects terminal records.

Extend list projection records with scalar conversion/closure fields and MaintenanceOpportunityConversionReference. Join Appointments or Quotes only for display data such as appointment start time or quote status; do not persist copied data. Add AwaitingCustomerCount to MaintenanceOpportunitySummary; urgency counts must include only open states.

Expose discard with a required body and delete obsolete complete/booking-request routes:

~~~csharp
public sealed record DiscardMaintenanceOpportunityRequest(string Reason);

group.MapPost("/{opportunityId:guid}/discard", async (
    Guid opportunityId,
    DiscardMaintenanceOpportunityRequest request,
    DiscardMaintenanceOpportunityUseCase useCase,
    CancellationToken cancellationToken) =>
    (await useCase.ExecuteAsync(opportunityId, new(request.Reason), cancellationToken))
        .ToHttpResult(() => Results.Ok()));
~~~

- [ ] **Step 4: Run the test to verify it passes**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunityLifecycleTests

Expected: PASS with closure audit values and a read-only conversion reference.

- [ ] **Step 5: Commit**

~~~powershell
git add src/VoltaMarcada.Application/MaintenanceOpportunities src/VoltaMarcada.Application/Abstractions/Persistence/IMaintenanceOpportunityRepository.cs src/VoltaMarcada.Infrastructure/Persistence/Repositories/MaintenanceOpportunityRepository.cs src/VoltaMarcada.Web/Endpoints/MaintenanceOpportunityEndpoints.cs src/VoltaMarcada.Application/DependencyInjection.cs tests/VoltaMarcada.IntegrationTests/MaintenanceOpportunityLifecycleTests.cs
git commit -m "Add maintenance opportunity conversion tracking"
~~~

### Task 5: Replace the Maintenance screen with compact opportunity cards

**Files:**
- Modify: src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor
- Modify: src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs only if discard introduces a new error code.
- Create: tests/VoltaMarcada.IntegrationTests/Components/MaintenanceOpportunitiesRenderingTests.cs

**Interfaces:**
- Consumes query parameter /orcamentos/registro?maintenanceOpportunityId={id}.
- Consumes the Task 4 discard use case, list item, AwaitingCustomerCount, and conversion reference.
- Produces navigation to /agenda?maintenanceOpportunityId={id} without pre-transitioning the opportunity.

- [ ] **Step 1: Write failing component rendering tests**

~~~csharp
[Fact]
public void RenderedPage_ShowsOpportunityActionsWithoutLegacyCompletionActions()
{
    var markup = RenderMaintenanceOpportunitiesPage();

    Assert.Contains("Agendar atendimento", markup);
    Assert.Contains("Gerar orçamento", markup);
    Assert.DoesNotContain(">Concluir<", markup);
    Assert.DoesNotContain(">Ignorar<", markup);
}
~~~

Add a second test confirming contact history belongs to a MudExpansionPanels/detail surface and is not a permanently expanded card section.

- [ ] **Step 2: Run the test to verify it fails**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunitiesRenderingTests

Expected: FAIL because the current page renders Concluir, Ignorar, and full contact history in every row.

- [ ] **Step 3: Write the minimal responsive MudBlazor UI**

Use outlined MudPaper cards inside a MudStack. Each card renders customer, equipment/service, prior/recommended dates, urgency chip, and status chip. Use the action row for Preparar contato, Agendar atendimento, and Gerar orçamento on open rows. Use MudMenu for secondary actions and a MudDialog with MudTextField for the mandatory discard reason.

Move notes, contacts, closure metadata, and concise conversion reference into MudExpansionPanels. Render an appointment date/time or quote status reference only; do not embed a calendar or quote form. Add summary/filter controls for Vencidas, Proximos 7 dias, Proximos 30 dias, and Aguardando cliente. ScheduleAsync only navigates; CreateQuoteAsync navigates with maintenanceOpportunityId.

- [ ] **Step 4: Run rendering and compile verification**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunitiesRenderingTests

Expected: PASS.

Run: dotnet build VoltaMarcada.sln --no-restore

Expected: Build succeeded with no errors.

- [ ] **Step 5: Commit**

~~~powershell
git add src/VoltaMarcada.Web/Components/Pages/MaintenanceOpportunities.razor src/VoltaMarcada.Web/Errors/OperationErrorMessages.cs tests/VoltaMarcada.IntegrationTests/Components/MaintenanceOpportunitiesRenderingTests.cs
git commit -m "Refactor maintenance opportunity screen"
~~~

### Task 6: Run the full regression suite

**Files:**
- Modify: only files required to correct a demonstrated failure from the commands below.

**Interfaces:**
- Consumes all lifecycle, appointment, quote, repository, endpoint, and UI interfaces from Tasks 1-5.
- Produces evidence that existing and new flows compile and pass the solution test suite.

- [ ] **Step 1: Run targeted maintenance coverage**

Run: dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~MaintenanceOpportunity

Expected: PASS for lifecycle, appointment, public booking, quote, migration catalog, and rendering tests.

- [ ] **Step 2: Run the full solution test suite**

Run: dotnet test VoltaMarcada.sln --no-restore

Expected: PASS with zero failed tests.

- [ ] **Step 3: Verify migration and source-control hygiene**

Run: dotnet ef migrations list --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web --no-build

Expected: output includes 20260902100000_RefactorMaintenanceOpportunityLifecycle.

Run: git diff --check; git status --short

Expected: no whitespace errors. Do not delete pre-existing local changes.

Do not create an empty commit when verification passes without corrections. A demonstrated regression correction belongs to the task that owns the affected interface, including that task's focused test and explicit commit file list.

