# Production Antiforgery and Status Pages Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Keep Data Protection keys in PostgreSQL and render the correct user-facing page for HTTP 400 and 404 without handling 401 as a status page.

**Architecture:** `AppDbContext` becomes the Data Protection key context backed by a new PostgreSQL table. Infrastructure configures a fixed ServiceSync Data Protection application name and persists keys through that context. The web host replaces global status-code re-execution with a selective handler that redirects only 400 and 404 responses to their intended Razor pages.

**Tech Stack:** .NET 10, ASP.NET Core Data Protection, Entity Framework Core 10, PostgreSQL, Blazor Web App.

**Spec:** `docs/superpowers/specs/2026-08-21-production-antiforgery-and-status-pages-design.md`

## Global Constraints

- Use the existing PostgreSQL database and EF Core migration workflow; do not introduce a new infrastructure service.
- Source code, migrations, logs, and comments must use English.
- Do not run builds, tests, or the application unless explicitly requested.
- The MVP testing rule does not require automated tests unless explicitly requested.
- Preserve 401 responses for authentication middleware; do not render a status page for them.

---

### Task 1: Persist Data Protection keys through EF Core

**Files:**

- Modify: `src/ServiceSync.Infrastructure/ServiceSync.Infrastructure.csproj`
- Modify: `src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs`
- Modify: `src/ServiceSync.Infrastructure/DependencyInjection.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260821180000_AddDataProtectionKeys.cs`
- Create: `src/ServiceSync.Infrastructure/Persistence/Migrations/20260821180000_AddDataProtectionKeys.Designer.cs`
- Modify: `src/ServiceSync.Infrastructure/Persistence/Migrations/AppDbContextModelSnapshot.cs`

**Interfaces:**

- Consumes: the existing `AppDbContext` configured through `AddDbContextFactory<AppDbContext>`.
- Produces: `IDataProtectionKeyContext.DataProtectionKeys` and `IDataProtectionBuilder` configuration shared by all web processes using the same database.

- [ ] **Step 1: Add the Data Protection EF Core provider package**

Add a package reference compatible with the existing EF Core 10.0.11 packages:

```xml
<PackageReference Include="Microsoft.AspNetCore.DataProtection.EntityFrameworkCore" Version="10.0.11" />
```

- [ ] **Step 2: Expose the Data Protection key entity from the database context**

Import `Microsoft.AspNetCore.DataProtection.EntityFrameworkCore`, implement `IDataProtectionKeyContext`, and add this set to `AppDbContext`:

```csharp
public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
```

- [ ] **Step 3: Configure shared Data Protection in infrastructure**

In `AddInfrastructure`, configure Data Protection after the database context registration. Use a fixed application name and the existing `AppDbContext`:

```csharp
services.AddDataProtection()
    .SetApplicationName("ServiceSync")
    .PersistKeysToDbContext<AppDbContext>();
```

- [ ] **Step 4: Add the `AddDataProtectionKeys` migration and snapshot change**

Create the migration with an identity integer primary key, a required `FriendlyName` text column, and a required `Xml` text column. Update the model snapshot with the `DataProtectionKey` entity mapped by the `DataProtectionKeys` context set.

```csharp
migrationBuilder.CreateTable(
    name: "DataProtectionKeys",
    columns: table => new
    {
        Id = table.Column<int>(type: "integer", nullable: false)
            .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
        FriendlyName = table.Column<string>(type: "text", nullable: false),
        Xml = table.Column<string>(type: "text", nullable: false)
    },
    constraints: table => table.PrimaryKey("PK_DataProtectionKeys", x => x.Id));
```

- [ ] **Step 5: Statically inspect the model and migration**

Confirm that the context implements `IDataProtectionKeyContext`, the migration name is English and descriptive, the table is created before the application begins serving requests, and no secret values are added to source control.

- [ ] **Step 6: Commit the infrastructure change**

```bash
git add src/ServiceSync.Infrastructure/ServiceSync.Infrastructure.csproj src/ServiceSync.Infrastructure/Persistence/AppDbContext.cs src/ServiceSync.Infrastructure/DependencyInjection.cs src/ServiceSync.Infrastructure/Persistence/Migrations
git commit -m "Persist data protection keys in PostgreSQL"
```

### Task 2: Render only intended status pages

**Files:**

- Modify: `src/ServiceSync.Web/Program.cs`
- Modify: `src/ServiceSync.Web/Components/Pages/Error.razor`

**Interfaces:**

- Consumes: empty HTTP error responses produced by the ASP.NET Core middleware pipeline.
- Produces: redirects from HTTP 400 to `/error`, redirects from HTTP 404 to `/not-found`, and unchanged HTTP 401 responses.

- [ ] **Step 1: Replace global status-code re-execution**

Remove `UseStatusCodePagesWithReExecute("/not-found", ...)` and register a selective handler before authentication and antiforgery middleware:

```csharp
app.UseStatusCodePages(async statusCodeContext =>
{
    var response = statusCodeContext.HttpContext.Response;
    var target = response.StatusCode switch
    {
        StatusCodes.Status400BadRequest => "/error",
        StatusCodes.Status404NotFound => "/not-found",
        _ => null
    };

    if (target is not null)
    {
        response.Redirect(target);
    }

    await Task.CompletedTask;
});
```

The default branch intentionally leaves 401 unchanged.

- [ ] **Step 2: Make the error page available after a rejected security token**

Replace the page authorization attribute with `AllowAnonymous`, use `AuthLayout`, and explain that the request could not be completed. Keep the action focused on reloading or returning to the application:

```razor
@page "/error"
@attribute [AllowAnonymous]
@layout AuthLayout
```

Use Brazilian Portuguese user-facing copy. Do not reveal internal antiforgery, database, or security implementation details.

- [ ] **Step 3: Statically review middleware behavior**

Verify the handler executes before `UseAuthentication`, `UseAuthorization`, and `UseAntiforgery`; only 400 and 404 receive redirects; 401 receives no status-page response; and `/error` is reachable without an authenticated cookie.

- [ ] **Step 4: Commit the status-page change**

```bash
git add src/ServiceSync.Web/Program.cs src/ServiceSync.Web/Components/Pages/Error.razor
git commit -m "Handle production status pages explicitly"
```

### Task 3: Review deployment behavior

**Files:**

- Modify: `docs/superpowers/specs/2026-08-21-production-antiforgery-and-status-pages-design.md` only if implementation differs from the approved design.

**Interfaces:**

- Consumes: automatic `ApplyDatabaseMigrationsAsync` startup behavior in `Program.cs`.
- Produces: a deployment-ready change that creates the key table at startup and lets existing users obtain new valid cookies after deployment.

- [ ] **Step 1: Review migration startup path**

Confirm `ApplyDatabaseMigrationsAsync` runs before the app starts handling requests, so production creates `DataProtectionKeys` without a separate manual database operation.

- [ ] **Step 2: Record post-deploy user action**

Document in the implementation handoff that existing browser cookies and form tokens from before deployment may be invalidated once; users should reload and sign in again after the deployment.

- [ ] **Step 3: Inspect the final diff**

Run:

```bash
git diff HEAD~2..HEAD --check
git status --short
```

Expected: no whitespace errors and no unexpected files.
