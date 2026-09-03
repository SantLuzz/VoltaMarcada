# Blazor Operation Scopes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Prevent disposed and concurrently used EF Core contexts in interactive Blazor by creating a short-lived DI scope for every UI operation.

**Architecture:** A stateless Web-layer executor creates an async child scope, resolves one scoped application service, awaits its operation, then disposes the scope. Repositories and use cases remain scoped, so a multi-repository use case retains its shared `AppDbContext` and transaction.

**Tech Stack:** .NET 10, ASP.NET Core Blazor Web App, EF Core 10, Microsoft.Extensions.DependencyInjection, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-03-blazor-operation-scopes-design.md`

## Global Constraints

- Keep Interactive Server prerendering enabled.
- Add no package, mediator, generic repository, schema change, or tenant identifier from the browser.
- Do not retain scoped use cases, repositories, authorizations, or tracked entities in component fields.
- Keep existing transactions and pass `CancellationToken` whenever an API accepts it.

---

### Task 1: Create and register an operation executor

**Files:**

- Create: `src/VoltaMarcada.Web/Operations/IComponentOperationExecutor.cs`
- Create: `src/VoltaMarcada.Web/Operations/ComponentOperationExecutor.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Operations/ComponentOperationExecutorTests.cs`

**Interfaces:**

```csharp
public interface IComponentOperationExecutor
{
    Task ExecuteAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull;

    Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull;
}
```

- [ ] **Step 1: Write failing lifetime tests**

```csharp
[Fact]
public async Task ExecuteAsync_ShouldKeepTheScopedServiceAliveUntilTheOperationCompletes()
{
    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
    var execution = executor.ExecuteAsync<TrackedScopedService>(async (service, _) =>
    {
        await completion.Task;
        Assert.False(service.IsDisposed);
    });

    completion.SetResult();
    await execution;
}

[Fact]
public async Task ExecuteAsync_ShouldUseAndDisposeADifferentScopedServiceForEachOperation()
{
    // Capture two probe identifiers and their disposal notifications.
    // Assert different identifiers and one disposal for each operation.
}
```

- [ ] **Step 2: Run the test to verify it fails**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~ComponentOperationExecutorTests"`

Expected: compile failure for the missing executor types.

- [ ] **Step 3: Implement the minimal executor**

```csharp
public sealed class ComponentOperationExecutor(IServiceScopeFactory scopeFactory)
    : IComponentOperationExecutor
{
    public Task ExecuteAsync<TService>(
        Func<TService, CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull =>
        ExecuteAsync<TService, object?>(async (service, token) =>
        {
            await operation(service, token);
            return null;
        }, cancellationToken);

    public async Task<TResult> ExecuteAsync<TService, TResult>(
        Func<TService, CancellationToken, Task<TResult>> operation,
        CancellationToken cancellationToken = default)
        where TService : notnull
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        return await operation(
            scope.ServiceProvider.GetRequiredService<TService>(),
            cancellationToken);
    }
}
```

Register the executor as singleton in `Program.cs`. The child scope contains the use case, tenant context, repositories, and one `AppDbContext`.

- [ ] **Step 4: Run the executor tests to verify they pass**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~ComponentOperationExecutorTests"`

Expected: all executor tests pass.

- [ ] **Step 5: Commit**

```powershell
git add src/VoltaMarcada.Web/Operations src/VoltaMarcada.Web/Program.cs tests/VoltaMarcada.IntegrationTests/Operations
git commit -m "feat: add Blazor operation scope executor"
```

### Task 2: Add a component source boundary and migrate internal read pages

**Files:**

- Create: `tests/VoltaMarcada.IntegrationTests/Components/ComponentOperationScopeBoundaryTests.cs`
- Modify: `Dashboard.razor`, `Customers.razor`, `Equipment.razor`, `Services.razor`, `Professionals.razor`, `WorkOrders.razor`, `MaintenanceOpportunities.razor`, `Quotes.razor`, and `Company.razor` under `src/VoltaMarcada.Web/Components/Pages/`.

**Interfaces:** Each listed component injects `IComponentOperationExecutor OperationExecutor`, never a use case or authorization service.

- [ ] **Step 1: Write a failing architecture regression test**

```csharp
[Theory]
[InlineData("Components/Pages/Dashboard.razor")]
[InlineData("Components/Pages/Customers.razor")]
public void InteractiveComponent_ShouldResolveApplicationServicesThroughOperationExecutor(string path)
{
    var source = File.ReadAllText(GetWebComponentPath(path));
    Assert.Contains("@inject IComponentOperationExecutor OperationExecutor", source);
    Assert.DoesNotMatch("@inject\\s+\\w*(UseCase|Authorization)\\s+", source);
}
```

Add every page listed in this task as `InlineData`.

- [ ] **Step 2: Run the boundary test to verify it fails**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~ComponentOperationScopeBoundaryTests"`

Expected: failures for pages with direct application-service injection.

- [ ] **Step 3: Replace direct injections and calls**

Preserve each page's loading flags, UI messages, and logging. Replace each call using this form:

```csharp
orders = await OperationExecutor.ExecuteAsync<GetWorkOrdersUseCase, IReadOnlyList<WorkOrderListItem>>(
    (useCase, cancellationToken) => useCase.ExecuteAsync(
        new GetWorkOrdersQuery(searchFilter, statusFilter),
        cancellationToken));
```

Dashboard resolves `WorkOrderAuthorization` and every dashboard query through independent executor calls. It must not inherit `CircuitIsolatedComponentBase`.

- [ ] **Step 4: Verify and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~ComponentOperationScopeBoundaryTests|FullyQualifiedName~MainLayoutTests"`

Expected: selected tests pass.

```powershell
git add src/VoltaMarcada.Web/Components/Pages tests/VoltaMarcada.IntegrationTests/Components
git commit -m "refactor: scope Blazor read operations"
```

### Task 3: Migrate authenticated forms, scheduling, and autosave

**Files:**

- Modify pages: `CustomerRegistration.razor`, `EquipmentRegistration.razor`, `ServiceRegistration.razor`, `ProfessionalRegistration.razor`, `WorkOrderRegistration.razor`, `QuoteRegistration.razor`, and `Schedule.razor`.
- Modify shared components: `AppointmentDetailsDialog.razor`, `AppointmentEditor.razor`, `ProfessionalAvailabilityPanel.razor`, `ScheduleBlockRequestPanel.razor`, `WorkOrderAppointmentsPanel.razor`, and `WorkOrderExecutionPanel.razor`.
- Modify: `ComponentOperationScopeBoundaryTests.cs`.
- Test: `AppointmentEditorTests.cs`, `ProfessionalAvailabilityPanelTests.cs`, and existing work-order tests.

**Interfaces:** One executor delegate resolves exactly one use case for one UI operation. Multi-repository commands continue inside their existing single application use case.

- [ ] **Step 1: Extend the boundary theory with all files in this task and verify red**

Run the Task 2 boundary-test command.

Expected: a failure for every retained direct `UseCase`, `Authorization`, or `CircuitIsolatedComponentBase` dependency.

- [ ] **Step 2: Migrate commands and lookups**

For results, retain existing success/error branches:

```csharp
var result = await OperationExecutor.ExecuteAsync<DeleteCustomerUseCase, OperationResult<Guid?>>(
    (useCase, cancellationToken) => useCase.ExecuteAsync(customer.Id, cancellationToken));
```

In `WorkOrderExecutionPanel`, remove the injected autosave use case. Resolve `SaveWorkOrderChecklistProgressUseCase` inside `FlushProgressAsync`, after `Task.Delay`, with the existing autosave token. Preserve cancellation, locking, and `isDisposed` checks.

- [ ] **Step 3: Run targeted workflow tests and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~AppointmentEditorTests|FullyQualifiedName~ProfessionalAvailabilityPanelTests|FullyQualifiedName~WorkOrder"`

Expected: selected tests pass.

```powershell
git add src/VoltaMarcada.Web/Components tests/VoltaMarcada.IntegrationTests/Components
git commit -m "refactor: scope Blazor workflow operations"
```

### Task 4: Migrate public interactive pages

**Files:**

- Modify: `PublicCompany.razor`, `PublicMaintenanceBooking.razor`, `PublicQuote.razor`, `PublicAppointmentRequestResponse.razor`, and `EmployeeInvitationAcceptance.razor`.
- Modify: `ComponentOperationScopeBoundaryTests.cs`.
- Test: `PublicBookingEquipmentTests.cs`, public quote tests, and `QuotePdfUseCaseTests.cs`.

**Interfaces:** Public use cases run through the executor; token and slug validation remain in Application. `PublicCompany` may retain `IDbContextFactory<AppDbContext>` only for its local state/city lookups that create and dispose a context inside the method.

- [ ] **Step 1: Add public pages to the failing boundary test**

Permit `IDbContextFactory<AppDbContext>` but reject direct use-case injection. Run the Task 2 command and verify it fails.

- [ ] **Step 2: Migrate all public use-case calls**

Use executor delegates for page reads, slot queries, contextual bookings, appointment-request responses, public quote reads/decisions, and invitation acceptance. Preserve rate-limit messages, public token behavior, and existing error handling.

- [ ] **Step 3: Run public-flow tests and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --filter "FullyQualifiedName~PublicBookingEquipmentTests|FullyQualifiedName~PublicQuote|FullyQualifiedName~QuotePdfUseCaseTests"`

Expected: selected tests pass.

```powershell
git add src/VoltaMarcada.Web/Components/Pages tests/VoltaMarcada.IntegrationTests
git commit -m "refactor: scope public Blazor operations"
```

### Task 5: Remove component-owned data scopes and complete verification

**Files:**

- Delete: `src/VoltaMarcada.Web/Components/CircuitIsolatedComponentBase.cs`
- Modify: `tests/VoltaMarcada.IntegrationTests/Components/CircuitIsolatedComponentBaseTests.cs`
- Modify: `tests/VoltaMarcada.IntegrationTests/Components/ComponentOperationScopeBoundaryTests.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Components/DashboardOperationScopeTests.cs`

**Interfaces:** Removes the obsolete component-owned scope API. Adds a dashboard regression test that keeps a blocking scoped authorization probe alive until the operation task completes.

- [ ] **Step 1: Write the failing Dashboard operation-lifetime test**

```csharp
[Fact]
public async Task DashboardLoad_ShouldNotDisposeTheScopeWhileAuthorizationIsRunning()
{
    // Start a blocking authorization probe through ComponentOperationExecutor.
    // Assert it is alive before completion and disposed only after completion.
}
```

- [ ] **Step 2: Delete the base and ownership tests**

Remove `CircuitIsolatedComponentBase.cs` and assertions that a component owns a scope. Retain source-boundary and operation-lifetime regression coverage.

- [ ] **Step 3: Verify all constraints and commit**

Run: `rg -n --glob '*.razor' '@inject .*UseCase|@inject .*Authorization|@inherits CircuitIsolatedComponentBase' src/VoltaMarcada.Web/Components`

Expected: no results.

Run: `git diff --check`

Expected: no output and exit code 0.

Run: `dotnet test VoltaMarcada.sln --no-restore --logger "console;verbosity=minimal"`

Expected: exit code 0 and no failed tests.

```powershell
git add src/VoltaMarcada.Web/Components tests/VoltaMarcada.IntegrationTests
git commit -m "refactor: remove component-owned database scopes"
```

