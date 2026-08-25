# Task 1 Report: Domain Tenant and Membership Model

## Files Changed

- `src/VoltaMarcada.Domain/Entities/Company.cs`
- `src/VoltaMarcada.Domain/Entities/CompanyUser.cs`
- `src/VoltaMarcada.Domain/Enums/CompanyUserRole.cs`
- `src/VoltaMarcada.Domain/ValueObjects/BrazilianDocument.cs`

## Implementation Decisions

- Replaced the legacy `Company` constructor with the required `Company.Create` factory. The factory trims required text fields, trims `LogoKey` when supplied, validates and stores a digits-only Brazilian document, generates a server-side GUID, and sets both UTC timestamp properties from the supplied `DateTimeOffset now` value.
- Kept the EF Core parameterless constructor and private setters on `Company` and `CompanyUser`.
- Added `CompanyUserRole` with the explicit values `Owner = 1` and `Employee = 2`.
- Added `CompanyUser.Create`, which generates the membership ID, accepts the supplied timestamp, and rejects empty user/company identifiers and undefined role values.
- Implemented `BrazilianDocument` as a dependency-free static value-object utility. It retains ASCII digits only, rejects documents with any repeated digit sequence, and validates CPF and CNPJ check digits before returning a normalized digits-only value.
- No automated tests were added, following the explicit task instruction.

## Build Command and Output Summary

Command run:

```text
dotnet build VoltaMarcada.sln
```

Result: exit code 1. Domain compilation completed, but the solution build failed in the existing Infrastructure project because `CompanyConfiguration.cs` references `Company.Name`, which Task 1 correctly replaced with the required company fields. Restore also emitted NU1900 warnings because package vulnerability sources could not be reached.

## Concerns

- `src/VoltaMarcada.Infrastructure/Persistence/Configurations/CompanyConfiguration.cs` must be updated by its owning scoped task to map the new `Company` properties and remove its obsolete `Name` mapping. This file was intentionally not modified because it is outside Task 1's allowed file list.
- No automated tests were run or created, as explicitly prohibited by the task.

## Review Fix Round 1

### Changes

- Updated `Company.Create` so `CreatedAtUtc` and `UpdatedAtUtc` are assigned from `now.ToUniversalTime()`.
- Updated `CompanyUser.Create` so `CreatedAtUtc` is assigned from `now.ToUniversalTime()`.

### Build Result

Command run:

```text
dotnet build VoltaMarcada.sln
```

Result: exit code 1. The Domain and Application projects built successfully. The solution remains blocked by the existing Infrastructure compilation error in `CompanyConfiguration.cs`, which still references the removed `Company.Name` property. NU1900 warnings were also emitted because the package vulnerability sources could not be reached.
