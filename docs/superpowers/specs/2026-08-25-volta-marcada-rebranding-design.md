# VoltaMarcada Rebranding Design

## Goal

Rename the existing codebase and product identity to VoltaMarcada without changing the PostgreSQL database schema, existing data, migration identities, public tokens, or other persisted security-sensitive values.

## Scope

- Rename the solution, source and test project directories, project files, project references, assembly names, and C# namespaces to `VoltaMarcada`.
- Rename active product copy, Docker image entry points, and repository documentation to `VoltaMarcada`.
- Update the namespaces in EF Core migration source files and the model snapshot so they compile under the renamed assemblies.
- Preserve migration class names and the values stored in `__EFMigrationsHistory`; no migrations are added, removed, recreated, or applied.
- Preserve connection strings, database names, table names, data, the existing `UserSecretsId`, Compose PostgreSQL container/volume/database identifiers, public tokens, cookie/data-protection compatibility settings, and existing binary logo assets.
- Rename the repository root directory only after internal references are complete. This is a filesystem move, not a database operation.

## Exclusions

- No `DROP DATABASE`, database recreation, schema/table rename, data migration, or production deployment.
- No application build, test execution, or application launch unless explicitly requested.
- No modification to the user-owned logo changes currently present in the worktree.

## Design

The rename is mechanical and repository-wide. All code paths and project metadata will use `VoltaMarcada`, including `VoltaMarcada.Domain`, `VoltaMarcada.Application`, `VoltaMarcada.Infrastructure`, `VoltaMarcada.Web`, and `VoltaMarcada.IntegrationTests`. The two development-environment compatibility identifiers are intentionally retained because changing either requires separately migrating secrets or local database storage.

Historic EF Core migrations remain semantically identical: only their namespace and referenced assembly names are updated. Their timestamped class names remain unchanged so EF Core continues to match the database migration history.

The physical PostgreSQL database remains untouched. A future database-name change is an independent operational procedure: close active connections, rename the database administratively, update the deployment connection string, and validate the deployment. It does not require dropping the database.

## Verification

- Use repository-wide searches to confirm that no `VoltaMarcada` references remain in tracked text files intended to be renamed.
- Inspect the solution and every project reference to confirm paths and project names are consistent.
- Review migration files to ensure migration class names and migration identifiers are unchanged.
- Do not run a build or tests under the repository's execution policy.
