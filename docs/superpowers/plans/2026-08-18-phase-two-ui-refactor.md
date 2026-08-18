# Phase Two UI Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Refactor the implemented ServiceSync Web interface into a coherent, responsive MudBlazor application aligned with the ServiceSync design system.

**Architecture:** Centralize visual tokens and MudBlazor defaults in a shared `ServiceSyncTheme`, make the authenticated and authentication layouts the only application shells, and use small shared presentation components for recurrent page structures. Preserve native endpoint POST contracts and existing domain/application behavior; this is a presentation-only refactor.

**Tech Stack:** .NET 10, ASP.NET Core Blazor Web App, MudBlazor 9.8, scoped CSS.

**Spec:** `docs/superpowers/specs/2026-08-18-phase-two-ui-refactor-design.md`

## Global Constraints

- Use MudBlazor for every UI element it supports; do not add another component library.
- Centralize the specified colors, typography, outlined field defaults, and component appearance in a `MudTheme`.
- Use `/logos/logo.png` unchanged for all authentication and onboarding branding.
- User-facing copy may be Brazilian Portuguese; all source code identifiers and technical documentation must be English.
- Preserve existing endpoint field names, native multipart posting, authentication behavior, tenancy, and routing.
- Do not run builds, tests, application hosts, or other executing validation unless the user explicitly asks.
- Do not add automated tests unless explicitly requested; perform the static reviews named in each task.

---

### Task 1: Establish the shared UI foundation

**Files:**
- Create: `src/ServiceSync.Web/Components/Theme/ServiceSyncTheme.cs`
- Modify: `src/ServiceSync.Web/Components/App.razor`
- Modify: `src/ServiceSync.Web/Components/_Imports.razor`
- Modify: `src/ServiceSync.Web/wwwroot/app.css`

**Interfaces:**
- Produces: `ServiceSyncTheme.Instance`, a `MudTheme` consumed by `MainLayout` and `AuthLayout`.
- Produces: global CSS custom properties and layout utility classes used only by shared shells and components.

- [ ] **Step 1: Add a centralized MudBlazor theme**

Create `ServiceSyncTheme` with an `Instance` property configured for the specified primary, secondary, app background, surface, border, text, and semantic colors. Configure Inter-first typography and component defaults for outlined inputs, 12px surface radii, and restrained elevations.

- [ ] **Step 2: Make global styles framework-neutral and token-based**

Replace Bootstrap template rules, gradients, raw form input rules, and legacy authentication CSS in `app.css` with font import/fallbacks, full-height document setup, ServiceSync CSS variables, focus behavior, and reusable shell helpers. Do not include component-specific colors that belong in the theme.

- [ ] **Step 3: Register shared imports and theme usage**

Add the Theme namespace to `_Imports.razor`. Update `App.razor` metadata so the theme-supported UI loads before application content and retains MudBlazor JavaScript and existing interactive rendering.

- [ ] **Step 4: Statically inspect the shared foundation**

Run `rg -n "#087e70|#06675c|Bootstrap|btn-primary|form-control" src/ServiceSync.Web/Components src/ServiceSync.Web/wwwroot/app.css` and review every match. Expected outcome: no active legacy green palette or Bootstrap template control styles remain in the refactored UI.

### Task 2: Refactor reusable form controls and authentication shell

**Files:**
- Modify: `src/ServiceSync.Web/Components/Layout/AuthLayout.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/PasswordInput.razor`
- Delete: `src/ServiceSync.Web/Components/Shared/PasswordInput.razor.css`
- Modify: `src/ServiceSync.Web/Components/Shared/BrazilianDocumentInput.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/BrazilianPhoneInput.razor`
- Modify: `src/ServiceSync.Web/Components/Shared/BrazilianPostalCodeInput.razor`

**Interfaces:**
- Consumes: `ServiceSyncTheme.Instance` from Task 1.
- Preserves: `Id`, `Name`, `Required`, `Autocomplete`, and `MinLength` parameter contracts of all shared inputs.
- Produces: MudBlazor-based controls whose rendered input names continue to match the account and onboarding endpoints.

- [ ] **Step 1: Make the authentication layout own the shared surface**

Render `MudThemeProvider` with `ServiceSyncTheme.Instance`, `MudPopoverProvider`, `MudDialogProvider`, and `MudSnackbarProvider` in `AuthLayout`. Provide the centered branded card shell with `/logos/logo.png`, responsive widths, and a body region for individual pages.

- [ ] **Step 2: Replace the password field implementation**

Replace the raw `<input>` and visibility `<button>` in `PasswordInput` with `MudTextField<string>`, binding the visibility state to `InputType.Password` or `InputType.Text`, and use a MudBlazor adornment icon/button. Keep the rendered field name and native attributes required by the endpoint.

- [ ] **Step 3: Replace Brazilian raw input implementations**

Replace each raw input with a `MudTextField<string>` using `@bind-Value` and `@bind-Value:after` for existing formatting. Preserve `name`, `id`, input mode, autocomplete, maximum length, requirement, and format methods.

- [ ] **Step 4: Remove obsolete field CSS and review contracts**

Delete the password scoped CSS. Compare the input parameter names used by Login and CompanyOnboarding with `AccountEndpoints.cs` and `CompanyOnboardingEndpoints.cs`; expected outcome: every posted key is unchanged.

### Task 3: Refactor authentication and company onboarding pages

**Files:**
- Modify: `src/ServiceSync.Web/Components/Pages/Account/Login.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Account/Register.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/CompanyOnboarding.razor`

**Interfaces:**
- Consumes: `AuthLayout` from Task 2.
- Consumes: shared formatted MudBlazor inputs from Task 2.
- Preserves: account form actions, `returnUrl` fields, query-string errors, onboarding multipart action, and onboarding field names.

- [ ] **Step 1: Convert Login to the shared authentication content hierarchy**

Remove duplicate card and text-brand markup. Supply a page title, heading, supporting description, `MudAlert` error surface, MudBlazor email field, existing `PasswordInput`, native hidden return URL, full-width `MudButton` submit action, and secondary registration navigation.

- [ ] **Step 2: Convert Register to the shared authentication content hierarchy**

Apply the same hierarchy to registration. Keep email, password, password confirmation, return URL, error behavior, form action, and direct login URL intact.

- [ ] **Step 3: Convert CompanyOnboarding fields and grouping**

Keep the native multipart `form` so file uploads remain endpoint-compatible. Replace visible raw text/select/file controls with MudBlazor controls; use a MudBlazor grid that collapses from two columns to one, logical company/contact/address section headings, `MudAlert` error feedback, and a full-width submit button. Bind state/city selection and refresh city options without changing `stateId` or `cityId` post names.

- [ ] **Step 4: Statically review form semantics**

Search `Login.razor`, `Register.razor`, and `CompanyOnboarding.razor` for raw visible `<input>`, `<select>`, and `<button>` elements. Expected outcome: only native hidden inputs remain, except where a documented multipart or MudBlazor technical limitation requires otherwise; labels, field identifiers, form actions, and required posting names remain present.

### Task 4: Refactor the authenticated shell and navigation

**Files:**
- Modify: `src/ServiceSync.Web/Components/Layout/MainLayout.razor`
- Modify: `src/ServiceSync.Web/Components/Layout/MainLayout.razor.css`
- Delete: `src/ServiceSync.Web/Components/Layout/NavMenu.razor`
- Delete: `src/ServiceSync.Web/Components/Layout/NavMenu.razor.css`

**Interfaces:**
- Consumes: `ServiceSyncTheme.Instance` from Task 1 and the existing company lookup behavior.
- Produces: the default authenticated layout used by `Routes.razor`.
- Preserves: dashboard route, authentication state check, company-name lookup, and POST logout endpoint/action.

- [ ] **Step 1: Apply the centralized theme and responsive MudBlazor shell**

Update `MainLayout` to use `MudThemeProvider Theme="@ServiceSyncTheme.Instance"`, providers, a responsive drawer at the medium breakpoint, a top bar with a labeled drawer toggle, bounded main content, and CSS classes based on design tokens.

- [ ] **Step 2: Replace branding and navigation presentation**

Use the official `/logos/logo.png` with preserved aspect ratio. Keep dashboard as the only current navigation destination, represent it with a `MudNavLink`, and remove unused Bootstrap starter navigation components and styles.

- [ ] **Step 3: Make logout MudBlazor-compatible without changing the POST contract**

Keep the native logout `form`, antiforgery token, and `/account/logout` action. Use a `MudButton` as the submit control with an explicit accessible label, then style only the form container in the shared shell CSS.

- [ ] **Step 4: Review layout responsiveness and stale styling**

Review drawer, app bar, content, and footer CSS at desktop and mobile media-query boundaries. Search the Layout folder for `sidebar`, `top-row`, `navbar`, and Bootstrap icon classes; expected outcome: no orphaned starter layout rules or components remain.

### Task 5: Create shared page feedback components and refactor all pages

**Files:**
- Create: `src/ServiceSync.Web/Components/Shared/PageHeader.razor`
- Create: `src/ServiceSync.Web/Components/Shared/EmptyState.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Dashboard.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/NotFound.razor`
- Modify: `src/ServiceSync.Web/Components/Pages/Error.razor`
- Modify: `src/ServiceSync.Web/Components/RedirectToLogin.razor`
- Modify: `src/ServiceSync.Web/Components/Layout/ReconnectModal.razor`

**Interfaces:**
- Produces: `PageHeader` with `Title`, `Description`, and optional `Actions` render fragment.
- Produces: `EmptyState` with `Icon`, `Title`, `Description`, and optional `Actions` render fragment.
- Consumes: the centralized theme and default authenticated layout.

- [ ] **Step 1: Add reusable page-header and empty-state components**

Create `PageHeader` with semantic heading markup and an optional action region that stacks on narrow screens. Create `EmptyState` with a non-color-only icon, title, description, and optional action region. Use MudBlazor typography, paper/surface, icon, and button primitives.

- [ ] **Step 2: Turn the Dashboard placeholder into an operationally framed empty state**

Use `PageHeader` for the page title and a concise description of pending operational information. Replace the placeholder sentence with `EmptyState` explaining that operational data will appear as Phase Two modules are enabled; do not invent data or add business behavior.

- [ ] **Step 3: Localize and present fallback states consistently**

Replace default error/not-found markup with accessible MudBlazor status surfaces and a recovery action. Replace the redirect paragraph with a centered loading indicator and localize visible reconnect modal labels to Brazilian Portuguese while retaining framework-required identifiers and JavaScript behavior.

- [ ] **Step 4: Run final static UI review**

Review all Razor and CSS files for: raw interactive controls that MudBlazor can replace, the canonical logo path, raw color literals outside the theme/global tokens, legacy Bootstrap classes, malformed Portuguese encoding, unlocalized fallback text, and preserved endpoint contracts. Document the exact review findings in the final handoff.
