# Error Handling Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace exceptions and feature-specific status enums used for expected failures with a common typed result contract, centralized HTTP translation, and consistent Blazor feedback.

**Architecture:** `ServiceSync.Application` owns framework-independent `OperationResult` contracts and feature-owned stable error definitions. Application use cases return expected failures explicitly, while `ServiceSync.Web` maps error categories to `ProblemDetails` and Portuguese presentation messages; unexpected exceptions are logged by a global handler or an asynchronous Blazor boundary.

**Tech Stack:** .NET 10, C#, ASP.NET Core Minimal APIs, Blazor Web App, MudBlazor, Entity Framework Core, PostgreSQL

**Spec:** `docs/superpowers/specs/2026-08-24-error-handling-design.md`

## Global Constraints

- Source code identifiers, internal messages, logs, and technical documentation must be written in English.
- Brazilian Portuguese is allowed only for user-facing Web UI text.
- Do not add frameworks or dependencies.
- Tenant-owned queries must continue filtering by both entity ID and current `CompanyId`.
- Expected validation, not-found, forbidden, and conflict outcomes must not throw exceptions.
- Unexpected exceptions and invariant violations must not be exposed to users.
- `OperationCanceledException` must propagate unchanged.
- Existing successful HTTP response status codes and payloads must remain unchanged.
- Preserve all pre-existing uncommitted scheduling changes.
- Do not add automated tests, run builds, run tests, start the application, or execute migrations because repository instructions prohibit those actions unless explicitly requested.
- Do not create implementation commits while unrelated uncommitted changes overlap the files being migrated; use scoped diffs and static checks as checkpoints.

---

### Task 1: Common Application Result Contract

**Files:**
- Create: `src/ServiceSync.Application/Abstractions/Operations/OperationErrorType.cs`
- Create: `src/ServiceSync.Application/Abstractions/Operations/OperationError.cs`
- Create: `src/ServiceSync.Application/Abstractions/Operations/OperationResult.cs`

**Interfaces:**
- Produces: `OperationErrorType`, `OperationError`, `OperationResult`, and `OperationResult<TValue>` for all later tasks.

- [x] **Step 1: Define broad error categories**

```csharp
namespace ServiceSync.Application.Abstractions.Operations;

public enum OperationErrorType
{
    Validation,
    NotFound,
    Forbidden,
    Conflict
}
```

- [x] **Step 2: Define a stable error value**

```csharp
namespace ServiceSync.Application.Abstractions.Operations;

public sealed record OperationError(
    string Code,
    OperationErrorType Type,
    string Description,
    string? Field = null);
```

- [x] **Step 3: Define non-generic and generic results**

```csharp
namespace ServiceSync.Application.Abstractions.Operations;

public sealed record OperationResult(bool IsSuccess, IReadOnlyList<OperationError> Errors)
{
    public OperationError? Error => Errors.Count == 0 ? null : Errors[0];
    public static OperationResult Success() => new(true, []);
    public static OperationResult Failure(params OperationError[] errors) =>
        new(false, Validate(errors));

    private static IReadOnlyList<OperationError> Validate(OperationError[] errors) =>
        errors.Length > 0
            ? errors
            : throw new ArgumentException("At least one operation error is required.", nameof(errors));
}

public sealed record OperationResult<TValue>(
    bool IsSuccess,
    TValue? Value,
    IReadOnlyList<OperationError> Errors)
{
    public OperationError? Error => Errors.Count == 0 ? null : Errors[0];
    public static OperationResult<TValue> Success(TValue value) => new(true, value, []);
    public static OperationResult<TValue> Failure(params OperationError[] errors) =>
        new(false, default, errors.Length > 0
            ? errors
            : throw new ArgumentException("At least one operation error is required.", nameof(errors)));
}
```

- [x] **Step 4: Perform static contract checks**

Run: `rg -n "namespace|public (sealed record|enum)|throw" src/ServiceSync.Application/Abstractions/Operations`

Expected: only the four result types and constructor-misuse guards are present; no ASP.NET Core, EF Core, or MudBlazor reference exists.

### Task 2: Feature Error Catalogs and Result Values

**Files:**
- Create: `src/ServiceSync.Application/Customers/CustomerErrors.cs`
- Create: `src/ServiceSync.Application/Equipment/EquipmentErrors.cs`
- Create: `src/ServiceSync.Application/Services/ServiceErrors.cs`
- Create: `src/ServiceSync.Application/Professionals/ProfessionalErrors.cs`
- Create: `src/ServiceSync.Application/CompanyOnboarding/CompanyOnboardingErrors.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/EmployeeInvitationErrors.cs`
- Create: `src/ServiceSync.Application/Scheduling/SchedulingErrors.cs`
- Create: `src/ServiceSync.Application/WorkOrders/WorkOrderErrors.cs`
- Create: `src/ServiceSync.Application/EmployeeInvitations/EmployeeInvitationOutcome.cs`
- Create: `src/ServiceSync.Application/WorkOrders/WorkOrderOperationValue.cs`

**Interfaces:**
- Consumes: `OperationError` and `OperationErrorType` from Task 1.
- Produces: feature-owned static error values; `EmployeeInvitationOutcome(string? Token, Guid? UserId)`; `WorkOrderOperationValue(Guid? WorkOrderId, Guid? PhotoId)`.

- [x] **Step 1: Add stable feature error definitions**

Each catalog uses the exact pattern below and includes one definition for every current feature result enum member except `Success`:

```csharp
using ServiceSync.Application.Abstractions.Operations;

namespace ServiceSync.Application.Customers;

public static class CustomerErrors
{
    public static readonly OperationError InvalidInput =
        new("Customer.InvalidInput", OperationErrorType.Validation, "Customer input is invalid.");
    public static readonly OperationError InvalidLocation =
        new("Customer.InvalidLocation", OperationErrorType.Validation, "Customer location is invalid.");
    public static readonly OperationError DuplicateDocument =
        new("Customer.DuplicateDocument", OperationErrorType.Conflict, "A customer with this document already exists.");
    public static readonly OperationError NotFound =
        new("Customer.NotFound", OperationErrorType.NotFound, "Customer was not found.");
    public static readonly OperationError HasLinkedEquipment =
        new("Customer.HasLinkedEquipment", OperationErrorType.Conflict, "Customer has linked equipment.");
}
```

Use the same `<Feature>.<Condition>` code convention for all other catalogs. Classify invalid input and invalid references as `Validation`, missing tenant records as `NotFound`, authorization failures as `Forbidden`, and duplicate/concurrent/state collisions as `Conflict`.

- [x] **Step 2: Add multi-value success contracts**

```csharp
namespace ServiceSync.Application.EmployeeInvitations;

public sealed record EmployeeInvitationOutcome(string? Token = null, Guid? UserId = null);
```

```csharp
namespace ServiceSync.Application.WorkOrders;

public sealed record WorkOrderOperationValue(Guid? WorkOrderId = null, Guid? PhotoId = null);
```

- [x] **Step 3: Verify code uniqueness and English diagnostics**

Run: `rg -o 'new\("[A-Za-z]+\.[A-Za-z]+' src/ServiceSync.Application -g '*Errors.cs' | Sort-Object | Group-Object | Where-Object Count -gt 1`

Expected: no duplicate code is returned.

Run: `rg -n "[áàâãéêíóôõúçÁÀÂÃÉÊÍÓÔÕÚÇ]" src/ServiceSync.Application -g '*Errors.cs'`

Expected: no output.

### Task 3: Migrate Application Use Cases

**Files:**
- Modify: all command use cases under `src/ServiceSync.Application/Customers/`
- Modify: all command use cases under `src/ServiceSync.Application/Equipment/`
- Modify: all command use cases under `src/ServiceSync.Application/Services/`
- Modify: all command use cases under `src/ServiceSync.Application/Professionals/`
- Modify: `src/ServiceSync.Application/CompanyOnboarding/CompleteCompanyOnboardingUseCase.cs`
- Modify: command use cases under `src/ServiceSync.Application/EmployeeInvitations/`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/AppointmentChangeUseCases.cs`
- Modify: `src/ServiceSync.Application/Scheduling/BlockRequestDecisionUseCases.cs`
- Modify: command use cases under `src/ServiceSync.Application/WorkOrders/`
- Delete after callers migrate: existing `*OperationResult.cs`, `CompleteCompanyOnboardingResult.cs`, `EmployeeInvitationResult.cs`, and result declarations in `SchedulingContracts.cs`

**Interfaces:**
- Consumes: `OperationResult<TValue>` and feature errors from Tasks 1-2.
- Produces: `Task<OperationResult<Guid?>>`, `Task<OperationResult<Guid>>`, `Task<OperationResult<EmployeeInvitationOutcome>>`, or `Task<OperationResult<WorkOrderOperationValue>>` use-case APIs.

- [x] **Step 1: Convert simple aggregate commands**

Use this exact transformation for customers, equipment, services, and professionals:

```csharp
public async Task<OperationResult<Guid?>> ExecuteAsync(
    CreateCustomerCommand command,
    CancellationToken cancellationToken = default)
{
    if (!(await validator.ValidateAsync(command, cancellationToken)).IsValid)
    {
        return OperationResult<Guid?>.Failure(CustomerErrors.InvalidInput);
    }

    // Existing tenant-aware loading and mutation remain unchanged.
    return OperationResult<Guid?>.Success(customer.Id);
}
```

Replace every enum/factory failure with its feature error. Do not catch `Exception`, `DomainException`, or `OperationCanceledException` in a use case.

- [x] **Step 2: Convert onboarding and invitations**

Return `OperationResult<Guid>` for onboarding and `OperationResult<EmployeeInvitationOutcome>` for invitation commands. Preserve repository outcome switches, but return feature errors for every expected outcome. Keep the default arm as `InvalidOperationException` because an unknown persistence enum is a programming/versioning error.

- [x] **Step 3: Convert scheduling**

Return `OperationResult<Guid>` from scheduling commands. Preserve current company scope checks, qualification checks, availability revalidation, concurrency translation, time-zone conversion, and the user's uncommitted scheduling edits. Convert all existing `SchedulingOperationResultCode` failures to `SchedulingErrors`.

- [x] **Step 4: Convert work orders**

Return `OperationResult<WorkOrderOperationValue>` from work-order commands. Preserve cleanup of an uploaded file after a failed save. Catch only the existing narrow persistence failure needed for cleanup, then rethrow; do not classify arbitrary storage/database errors as business failures.

- [x] **Step 5: Remove obsolete result declarations**

Delete feature result files only after `rg` shows that no caller references their types or enums. Remove scheduling result declarations from `SchedulingContracts.cs` without changing its request/DTO contracts.

- [x] **Step 6: Verify expected failures are explicit**

Run: `rg -n "OperationResultCode|CompleteCompanyOnboardingResult|EmployeeInvitationResult|SchedulingOperationResult|WorkOrderOperationResult" src/ServiceSync.Application`

Expected: no obsolete feature result reference remains.

Run: `rg -n "catch \((DomainException|Exception)|throw new DomainException" src/ServiceSync.Application`

Expected: no catch used for expected control flow and no application-created `DomainException`.

### Task 4: Centralized Web Mapping and Unexpected Exception Boundary

**Files:**
- Create: `src/ServiceSync.Web/Errors/OperationErrorMessages.cs`
- Create: `src/ServiceSync.Web/Errors/OperationResultHttpExtensions.cs`
- Create: `src/ServiceSync.Web/Errors/UnexpectedExceptionHandler.cs`
- Modify: `src/ServiceSync.Web/Program.cs`

**Interfaces:**
- Consumes: common application result contracts and stable feature error codes.
- Produces: `ToHttpResult(Func<IResult>)`, `ToHttpResult<TValue>(Func<TValue, IResult>)`, and `IExceptionHandler` registration.

- [x] **Step 1: Centralize Portuguese messages**

Create a Web-only dictionary keyed by stable application error code. Expose:

```csharp
public static string GetMessage(string code) => Messages.GetValueOrDefault(
    code,
    "Não foi possível concluir a operação. Tente novamente.");
```

Include every code declared in Task 2. Do not return application error descriptions to the user.

- [x] **Step 2: Map operation errors to ProblemDetails**

```csharp
public static IResult ToHttpResult<TValue>(
    this OperationResult<TValue> result,
    Func<TValue, IResult> onSuccess)
{
    if (result.IsSuccess && result.Value is not null)
    {
        return onSuccess(result.Value);
    }

    var error = result.Error ?? UnexpectedError;
    var problem = new ProblemDetails
    {
        Status = GetStatusCode(error.Type),
        Title = OperationErrorMessages.GetMessage(error.Code)
    };
    problem.Extensions["code"] = error.Code;
    AddFieldErrors(problem, result.Errors);
    return Results.Problem(problem);
}
```

The non-generic overload follows the same mapping. `GetStatusCode` maps Validation/NotFound/Forbidden/Conflict to 400/404/403/409.

- [x] **Step 3: Add an unexpected exception handler**

Implement `IExceptionHandler.TryHandleAsync`, log the exception with `TraceIdentifier`, set status 500, and write generic `ProblemDetails` containing only a safe Portuguese title and `traceId`. Do not catch or convert cancellation.

- [x] **Step 4: Register the boundary**

In `Program.cs`, call `AddExceptionHandler<UnexpectedExceptionHandler>()` and `AddProblemDetails()`. In non-development environments, replace the `/error` exception re-execution with `app.UseExceptionHandler()` while retaining HSTS and status-code pages.

- [x] **Step 5: Verify boundary safety**

Run: `rg -n "exception\.Message|Exception\.Message|stack|StackTrace" src/ServiceSync.Web`

Expected: no exception detail is added to HTTP responses or rendered UI.

### Task 5: Migrate Minimal API Endpoints

**Files:**
- Modify: `src/ServiceSync.Web/Endpoints/CompanyOnboardingEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/CustomerEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/EquipmentEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/ServiceEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/ProfessionalEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/EmployeeInvitationEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/AppointmentEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/ScheduleBlockRequestEndpoints.cs`
- Modify: `src/ServiceSync.Web/Endpoints/WorkOrderEndpoints.cs`

**Interfaces:**
- Consumes: `OperationResultHttpExtensions` from Task 4 and migrated use-case signatures from Task 3.
- Produces: unchanged success responses and consistent RFC 7807 expected-failure responses.

- [x] **Step 1: Replace endpoint result switches**

Use the shared mapper and keep success callbacks local:

```csharp
var result = await useCase.ExecuteAsync(command, cancellationToken);
return result.ToHttpResult(customerId =>
    Results.Created($"/customers/{customerId}", new { customerId }));
```

For delete/update actions use `Results.NoContent()` or the existing success response. Remove feature-specific `ToResult`, `GetErrorMessage`, `InvalidInput`, and `UnexpectedResult` helpers after their final caller is migrated.

- [x] **Step 2: Preserve request-shape guards**

Null request-body and file-size checks may return a shared validation `ProblemDetails` directly because they occur before a use case can be invoked. Use the same `code`, status, and Portuguese message conventions.

- [x] **Step 3: Verify endpoint consistency**

Run: `rg -n "OperationResultCode|Results\.BadRequest\(new \{ error|Results\.Conflict\(new \{ error|GetErrorMessage|UnexpectedResult" src/ServiceSync.Web/Endpoints`

Expected: no old result enums, anonymous error payloads, or duplicated mapping switches remain.

### Task 6: Migrate Blazor Expected-Error Handling

**Files:**
- Modify result consumers under `src/ServiceSync.Web/Components/Pages/`
- Modify result consumers under `src/ServiceSync.Web/Components/Shared/`
- Create only if at least two identical mappings remain: `src/ServiceSync.Web/Errors/OperationResultUiExtensions.cs`

**Interfaces:**
- Consumes: common application results and `OperationErrorMessages`.
- Produces: inline field/form errors and snackbars without feature result enum switches.

- [x] **Step 1: Replace enum branches with common result checks**

Use:

```csharp
var result = await useCase.ExecuteAsync(command, cancellationToken);
if (!result.IsSuccess)
{
    submitError = OperationErrorMessages.GetMessage(result.Error?.Code ?? string.Empty);
    return;
}
```

Use `result.Value` only after `IsSuccess` is true. Keep field validation close to MudBlazor inputs and preserve existing loading/disabled/finally behavior.

- [x] **Step 2: Keep unexpected UI catches narrow and safe**

Existing `catch (Exception exception)` blocks may remain only at asynchronous component boundaries. Each must log structured operation context and show a generic Portuguese fallback; it must not inspect exception messages for business branching or display them.

- [x] **Step 3: Remove duplicated UI error switches**

Delete feature `GetOperationError` and equivalent methods after all callers use `OperationErrorMessages`. Add a small Web-only extension only when the same null-safe mapping appears in at least two components.

- [x] **Step 4: Verify presentation safety**

Run: `rg -n "OperationResultCode|CompleteCompanyOnboardingResultCode|EmployeeInvitationResultCode|SchedulingOperationResultCode|WorkOrderOperationResultCode" src/ServiceSync.Web`

Expected: no old result enum remains.

Run: `rg -n "exception\.Message|Exception\.Message" src/ServiceSync.Web/Components`

Expected: no raw exception text is displayed.

### Task 7: Domain Exception Audit and Final Static Verification

**Files:**
- Modify only domain methods where a predictable user-driven failure still lacks an application precondition.
- Preserve: `src/ServiceSync.Domain/Exceptions/DomainException.cs`
- Modify: `docs/superpowers/plans/2026-08-24-error-handling-implementation.md` checkbox state as work completes.

**Interfaces:**
- Consumes: all migrated application preconditions.
- Produces: a documented distinction between invariant exceptions and expected operation errors.

- [x] **Step 1: Audit every domain exception site**

Run: `rg -n -C 2 "throw new DomainException" src/ServiceSync.Domain`

For each guard, confirm at least one of these is true: it protects construction from programmer misuse; it protects an impossible state transition after application prechecks; or an application validator/precondition handles the corresponding user-driven input. Add a missing application precondition when none is true. Do not weaken entity invariants.

- [x] **Step 2: Scan for expected-flow exception handling**

Run: `rg -n "catch \((DomainException|DbUpdateException|Exception)" src/ServiceSync.Application src/ServiceSync.Web`

Expected: only narrow infrastructure translation/cleanup catches and generic UI/HTTP unexpected-failure boundaries remain.

- [x] **Step 3: Scan obsolete contracts and ad hoc HTTP errors**

Run: `rg -n "OperationResultCode|CompleteCompanyOnboardingResult|EmployeeInvitationResult|SchedulingOperationResult|WorkOrderOperationResult|new \{ error =" src`

Expected: no obsolete feature result contract or anonymous error response remains.

- [x] **Step 4: Review all scoped changes without executing the application**

Run: `git diff --check`

Expected: no whitespace errors.

Run: `git diff --stat`

Expected: only error-handling files and directly migrated consumers are listed alongside the user's preserved pre-existing scheduling changes.

- [x] **Step 5: Report verification limits**

State explicitly in the handoff that no build, test, migration, or application launch was run because `AGENTS.md` prohibits it for normal task execution.
