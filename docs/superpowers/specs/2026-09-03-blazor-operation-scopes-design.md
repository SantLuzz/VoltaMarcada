# Blazor Operation Scopes Design

## Goal

Ensure that every interactive Blazor operation uses a short-lived dependency-injection scope and an `AppDbContext` that is valid for the complete operation, while preserving server-side prerendering and existing application transactions.

## Problem

`AppDbContext` is registered through `AddDbContextFactory<AppDbContext>`, but the current repositories and application use cases receive `AppDbContext` through scoped DI. Components that inject those use cases retain the circuit-scoped context. The temporary `CircuitIsolatedComponentBase` uses `OwningComponentBase` to create a component scope, but prerendered components dispose that scope while asynchronous database work can still be in progress. Production evidence shows `OwningComponentBase.DisposeAsyncCore` disposing `AppDbContext` while Npgsql is connecting, followed by an `ObjectDisposedException` in Dashboard initialization.

## Decision

Keep prerendering enabled. Do not create a DI scope for the lifetime of a component and do not disable SSR. Instead, create a DI scope for each interactive application operation.

An operation is one awaited component action, such as initial data loading, a filter refresh, a form submission, a dialog action, an autosave flush, or an appointment update. The operation scope is created immediately before resolving the use case and disposed only after the returned task completes.

HTTP endpoints remain unchanged: ASP.NET Core already provides one scope per request. Public components that are interactive must also use the operation executor for their post-render actions; direct `IDbContextFactory` use for small read-only lookups remains valid.

## Architecture

### Component operation executor

Create a Web-layer service, `ComponentOperationExecutor`, with a small interface such as:

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

Its implementation uses `IServiceScopeFactory.CreateAsyncScope()`, resolves `TService` from the child scope, awaits the delegate, and disposes the child scope afterward. No service instance resolved from the child scope is stored in component state.

Use cases and repositories remain scoped. Therefore, a use case and all of its repository dependencies share one `AppDbContext` for that operation. Existing transaction code in `AppointmentRepository`, `QuoteRepository`, `MaintenanceOpportunityRepository`, `CompanyOnboardingRepository`, and `EmployeeInvitationAccountService` remains transactional without changing persistence abstractions.

### Component rules

Interactive components must inject `IComponentOperationExecutor` instead of scoped use cases and authorizations. Each use case call is expressed through the executor, for example:

```csharp
customers = await OperationExecutor.ExecuteAsync<GetCustomersUseCase, IReadOnlyList<CustomerDto>>(
    (useCase, cancellationToken) => useCase.ExecuteAsync(new GetCustomersQuery(searchFilter), cancellationToken));
```

When a UI workflow intentionally needs several use-case calls to represent one atomic business action, it must be represented by one application use case rather than resolving multiple services in the component. Read-model refreshes may use independent operation scopes.

`CircuitIsolatedComponentBase` is removed after its current consumers are migrated. Components must not inherit `OwningComponentBase` merely to manage data access.

### Authentication and tenancy

`ICurrentUser` and `ICurrentCompany` are resolved inside the operation scope. The executor must use the circuit's service provider as the source of child scopes so its `IHttpContextAccessor` and authenticated principal remain available in the normal Blazor Server execution context. Tenant checks stay inside application use cases; neither the component nor the executor accepts a `CompanyId`.

## Error handling and cancellation

Components retain their existing user-facing error handling and logging. The executor propagates exceptions unchanged so component-specific messages remain intact. Component disposal and user cancellation are passed as `CancellationToken`s where a component already maintains one; the executor always disposes its scope after the delegate returns, faults, or is cancelled.

No background operation may capture a resolved use case or repository. The work-order autosave must invoke the executor inside `FlushProgressAsync`, after the delay, rather than retaining its injected use case.

## Migration scope

1. Add and register the operation executor plus focused tests that prove scopes are distinct per operation and remain alive until an awaited operation completes.
2. Migrate all authenticated interactive pages and shared components that inject application use cases, authorizations, or repositories.
3. Migrate interactive public booking and quote components that invoke application use cases after hydration.
4. Remove `CircuitIsolatedComponentBase` and its obsolete ownership tests.
5. Add regression coverage for Dashboard prerendering and a multi-repository write flow to demonstrate that transactions still share one context inside a single use case.

## Non-goals

- Do not change domain rules, tenant authorization behavior, route design, database schema, or public page URLs.
- Do not replace existing repositories with one `DbContext` per repository method.
- Do not introduce a generic repository, mediator, event bus, or third-party dependency.
- Do not disable prerendering.

## Acceptance criteria

- No interactive component holds a scoped application use case, repository, or authorization service across operations.
- No component inherits `OwningComponentBase` for persistence isolation.
- A component operation disposes its child DI scope only after the use case task has completed.
- Existing multi-repository transactions continue to commit or roll back atomically.
- Dashboard can prerender and initialize without disposing an active `AppDbContext`.
- The full automated test suite passes.
