# Task 1 Review Package

Git metadata is unavailable in this workspace, so a commit diff cannot be generated.

## Changed-file review scope

- `src/ServiceSync.Domain/Entities/Company.cs`
- `src/ServiceSync.Domain/Entities/CompanyUser.cs`
- `src/ServiceSync.Domain/Enums/CompanyUserRole.cs`
- `src/ServiceSync.Domain/ValueObjects/BrazilianDocument.cs`

## Baseline limitation

The previous `Company` entity had only `Id`, `Name`, and `CreatedAtUtc`; the other three files did not exist. The reviewer must inspect the current versions of the four scoped files against the Task 1 brief. No other files are in scope.

## Build evidence

`dotnet build ServiceSync.sln` was run. It reached Infrastructure and failed because the unchanged `CompanyConfiguration` still refers to the deliberately removed `Company.Name` property. Task 3 owns that configuration update.

## Fix round 1

The only amended lines are the three UTC-named timestamp assignments in `Company.Create` and `CompanyUser.Create`. They now call `now.ToUniversalTime()` before persistence. The full solution build was repeated and has the same expected out-of-scope Infrastructure mapping failure.
