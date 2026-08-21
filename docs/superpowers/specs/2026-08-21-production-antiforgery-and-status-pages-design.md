# Production Antiforgery and Status Pages Design

## Goal

Prevent valid production form submissions from being rejected after an application restart or when a request is handled by another application instance, and present clear HTTP status pages.

## Scope

- Persist ASP.NET Core Data Protection keys in the existing PostgreSQL database.
- Use a fixed application name so every ServiceSync process uses the same Data Protection purpose chain.
- Add an intentional EF Core migration for the Data Protection key table.
- Replace the global status-code re-execution behavior.

## Data Protection

`AppDbContext` will implement the Data Protection EF Core key context and expose a `DataProtectionKeys` set. Infrastructure configuration will register Data Protection with `PersistKeysToDbContext<AppDbContext>()` and a fixed ServiceSync application name.

This makes the authentication and antiforgery cryptographic keys available to every application process connected to the same database. It prevents an app restart or a second instance from invalidating a form token created by another instance.

The implementation will use the Microsoft ASP.NET Core Data Protection EF Core provider and a descriptive `AddDataProtectionKeys` migration.

## Status Pages

The existing global `UseStatusCodePagesWithReExecute("/not-found")` behavior re-executes every empty error response as the 404 page. It will be replaced with a status-code handler that:

- redirects 400 responses to an anonymous `/error` page with a clear retry instruction;
- leaves 401 responses untouched so authentication middleware can handle them without rendering a status page;
- redirects 404 responses to the existing `/not-found` page;
- leaves all other status responses unchanged.

The error page will be available without authentication, since an antiforgery failure can coincide with an expired or undecryptable authentication cookie.

## Validation

The repository's MVP rule does not require new automated tests unless explicitly requested. Validation will use static review of the registration, EF model, and HTTP middleware ordering. No build, test, or application launch will be run unless explicitly requested.

