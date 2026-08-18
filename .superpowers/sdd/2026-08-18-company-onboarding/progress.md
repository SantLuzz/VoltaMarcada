# SDD ledger — plan: D:\Code\ServiceSync\docs\superpowers\plans\2026-08-18-company-onboarding.md

| Scope | Produces / Consumes | Finding | Ruling |
| --- | --- | --- | --- |
| Task 1 → Task 2 | `Company`, `CompanyUser`, `BrazilianDocument` | Task 2 uses the exact APIs produced by Task 1. | Consistent. |
| Task 2 → Task 3 | `ICompanyOnboardingRepository` | Task 3 implements Task 2's contract. | Consistent. |
| Task 3 → Task 4 | EF repository and membership storage | Task 4 requires the storage APIs and identity mapping produced by Task 3. | Consistent. |
| Task 4 → Task 5 | Form endpoints and contexts | Task 5 consumes the named endpoints and redirect logic from Task 4. | Consistent. |
| Task 5 → Task 6 | Complete web flow and migration | Task 6 manually verifies the completed flow. | Consistent. |
| Task 1 | Domain API and no-test constraint | The plan requests a build rather than automated tests, matching the explicit product decision. | Consistent. |
| Task 2 | Slug and document ownership | CPF/CNPJ validation is domain behavior; slug normalization is application behavior. | Consistent. |
| Task 3 | Transaction responsibility | The use case coordinates business behavior and Infrastructure owns the EF transaction. | Consistent. |
| Task 4 | Current company source | The current user is resolved from claims and company from membership storage. | Consistent. |
| Task 5 | Cookie creation | Forms post to normal HTTP endpoints because Identity cookie headers cannot reliably be written from an interactive circuit event. | Consistent. |

Ruling: The workspace has no Git metadata, so worktree setup, commit-based review packages, and branch cleanup cannot be performed. Reviews will inspect the task's changed files and build output instead — cost if wrong: review may miss an unrelated concurrent change.

Task 1: fix round 1/5 (2 addressed, 0 open — UTC timestamp normalization; no commits available)
Task 1: complete (no commits available, review clean)

Task 2: Ruling: When both the base slug and its document-suffixed candidate exist, return `SlugAlreadyExists` rather than generate another suffix — the approved specification explicitly says to show a conflict because a unique document makes this collision impossible in normal data; cost if wrong: a corrupted or legacy database could require manual company-name correction.
Task 2: complete (no commits available, review clean after specification ruling)

Task 3: Ruling: Defer EF migration scaffolding until Task 4 registers the Web current-user context required for design-time host construction — source and model configuration already build; cost if wrong: the final schema migration could require an extra correction after endpoint integration.
Task 3: source review clean; migration verification deferred to Task 4 follow-up.
Task 3: migration generated during Task 4; requires final inspection with EF 10 tooling because the local CLI is EF 9.0.1.
