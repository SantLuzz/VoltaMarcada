# VoltaMarcada Rebranding Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rename the repository and its active product identity to VoltaMarcada without changing persisted database state.

**Architecture:** This is a mechanical source identity rename. Project paths, project metadata, namespaces, and active product copy move to `VoltaMarcada`; EF Core migration IDs and the database schema remain unchanged.

**Tech Stack:** .NET 10, ASP.NET Core, Blazor, EF Core, PostgreSQL, Docker Compose.

**Spec:** `docs/superpowers/specs/2026-08-25-volta-marcada-rebranding-design.md`

## Global Constraints

- Source code and technical documentation use English identifiers and comments.
- Preserve PostgreSQL schema, data, connection strings, migration class names, migration history, the existing `UserSecretsId`, and Compose PostgreSQL identifiers/volume name.
- Do not run builds, tests, or the application unless explicitly requested.
- Do not alter the existing user-owned logo files in `src/VoltaMarcada.Web/wwwroot/logos`.
- Do not commit unrelated worktree changes.

---

### Task 1: Capture the safe rename boundary

**Files:**
- Modify: `docs/superpowers/specs/2026-08-25-volta-marcada-rebranding-design.md`
- Modify: `docs/superpowers/plans/2026-08-25-volta-marcada-rebranding.md`

**Interfaces:**
- Consumes: current Git worktree state and the existing solution/project layout.
- Produces: an explicit inventory of artifacts that may be renamed and persisted artifacts that must not change.

- [ ] **Step 1: Record user-owned worktree changes**

Run: `git status --short`

Expected: the pre-existing `logo.png` modification and `icone-fundo.png` untracked file are visible and excluded from the rename.

- [ ] **Step 2: Inventory textual product references**

Run: `rg -l --hidden --glob '!.git/**' --glob '!**/bin/**' --glob '!**/obj/**' 'VoltaMarcada'`

Expected: solution metadata, project files, source files, Razor files, Docker files, documentation, and migrations are enumerated.

- [ ] **Step 3: Preserve migration identities**

Inspect: `src/VoltaMarcada.Infrastructure/Persistence/Migrations/*.cs`

Expected: only namespace and referenced assembly identity may change; timestamped migration class names and `Migration` attributes remain exactly unchanged.

### Task 2: Rename solution and project identities

**Files:**
- Rename: `VoltaMarcada.sln` to `VoltaMarcada.sln`
- Rename: `src/VoltaMarcada.Domain/VoltaMarcada.Domain.csproj` to `src/VoltaMarcada.Domain/VoltaMarcada.Domain.csproj`
- Rename: `src/VoltaMarcada.Application/VoltaMarcada.Application.csproj` to `src/VoltaMarcada.Application/VoltaMarcada.Application.csproj`
- Rename: `src/VoltaMarcada.Infrastructure/VoltaMarcada.Infrastructure.csproj` to `src/VoltaMarcada.Infrastructure/VoltaMarcada.Infrastructure.csproj`
- Rename: `src/VoltaMarcada.Web/VoltaMarcada.Web.csproj` to `src/VoltaMarcada.Web/VoltaMarcada.Web.csproj`
- Rename: `tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj` to `tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj`

**Interfaces:**
- Consumes: existing solution GUIDs and project relationships.
- Produces: a solution whose project display names and `ProjectReference` paths all use `VoltaMarcada` while retaining solution project GUIDs.

- [ ] **Step 1: Rename directories and project files**

Rename the five project directories and their `.csproj` files using Git-aware moves where available. Preserve every non-project file below those directories.

- [ ] **Step 2: Update solution entries**

In `VoltaMarcada.sln`, replace each project display name and relative project path with its `VoltaMarcada` counterpart. Keep all project GUIDs and solution configuration sections unchanged.

- [ ] **Step 3: Update project references**

In every `.csproj`, replace old project paths with the corresponding `VoltaMarcada.*` paths. Preserve the Web project's existing `UserSecretsId` so current development secrets remain available; do not inspect or copy secrets.

- [ ] **Step 4: Validate project metadata statically**

Run: `rg -n 'VoltaMarcada\.(Domain|Application|Infrastructure|Web|IntegrationTests)' --glob '*.sln' --glob '*.csproj'`

Expected: no matches.

### Task 3: Rename source namespaces and active product copy

**Files:**
- Modify: all tracked source, Razor, Razor JavaScript, configuration, and text documentation files containing the prior product identity.
- Rename: source/document filenames whose own name contains the prior product identity, including the theme class file.

**Interfaces:**
- Consumes: the renamed project folders from Task 2.
- Produces: compilable source imports/namespaces under `VoltaMarcada.*` and product-facing copy that names VoltaMarcada.

- [ ] **Step 1: Replace technical namespace and assembly references**

Replace the prior product identity with `VoltaMarcada` in C# namespaces, `using` directives, fully-qualified type names, Razor imports, generated migration namespace declarations, and configuration values that identify the application.

- [ ] **Step 2: Rename affected source filenames**

Rename files containing the old identity, including the theme class file, and update all source references to their new names.

- [ ] **Step 3: Replace active product copy and documentation names**

Replace the product name in README, business plans, design specifications, implementation plans, UI instructions, and user-facing UI text. Rename documentation filenames containing `VoltaMarcada` to `VoltaMarcada` where appropriate.

- [ ] **Step 4: Preserve database compatibility**

Do not change `ConnectionStrings:DefaultConnection`, database/table mappings, migration class names, migration timestamps, migration attributes, or values in `__EFMigrationsHistory`.

### Task 4: Update operational metadata and repository branding

**Files:**
- Modify: `Dockerfile`
- Modify: `docker-compose.yml`
- Modify: `src/VoltaMarcada.Web/AGENTS.md`
- Modify: root `AGENTS.md`, `README.md`, scripts, and other tracked documentation/text files containing the old product name.

**Interfaces:**
- Consumes: renamed web project path and output assembly name.
- Produces: local container configuration and repository guidance that refer to VoltaMarcada.

- [ ] **Step 1: Update Docker build and entry point paths**

Change Docker restore/publish paths to `src/VoltaMarcada.Web/VoltaMarcada.Web.csproj` and the runtime entry point to `VoltaMarcada.Web.dll`.

- [ ] **Step 2: Preserve local Compose database compatibility identifiers**

Leave the Compose PostgreSQL service, container, database/user defaults, and volume labels unchanged. They identify existing local data; renaming them is a separate data migration and is excluded from this rebranding.

- [ ] **Step 3: Update repository and frontend guidance**

Replace all current product identity references in both AGENTS files and tracked documentation, including logo and theme guidance. Keep domain terminology in English and UI copy in Brazilian Portuguese where it already is localized.

### Task 5: Static verification and final repository folder move

**Files:**
- Inspect: all tracked text files, project metadata, and migration files.
- Rename: repository folder `D:\Code\VoltaMarcada` to `D:\Code\VoltaMarcada` only after all internal paths are verified.

**Interfaces:**
- Consumes: all completed rename tasks.
- Produces: a clean static rename report and a repository root path consistent with the new identity.

- [ ] **Step 1: Verify no old textual identity remains**

Run: `rg -n --hidden --glob '!.git/**' --glob '!**/bin/**' --glob '!**/obj/**' 'VoltaMarcada'`

Expected: no matches in active tracked text. Binary asset contents are not inspected or changed.

- [ ] **Step 2: Verify database migration identity preservation**

Run: `rg -n '\[Migration\(' 'src/VoltaMarcada.Infrastructure/Persistence/Migrations'`

Expected: the same migration identifiers present before the rename are still present.

- [ ] **Step 3: Review the resulting diff**

Run: `git status --short` and `git diff --check`

Expected: only intentional rename/content changes plus the pre-existing logo changes; no whitespace errors.

- [ ] **Step 4: Rename the repository root directory**

After tools no longer hold the current folder open, move `D:\Code\VoltaMarcada` to `D:\Code\VoltaMarcada`. Do not execute this move while the application, IDE, Docker bind mount, or agent shell has an active lock.
