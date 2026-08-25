# Phase Two UI Refactor Design

## Scope

Refactor every currently implemented VoltaMarcada Web screen and its shared presentation primitives. The work covers the authenticated application shell, dashboard, authentication, company onboarding, shared inputs, fallback pages, global styling, and component providers. It does not add Phase Two business modules or change endpoint contracts.

## Objectives

- Establish the mandatory MudBlazor-based VoltaMarcada UI system.
- Remove Bootstrap-era and raw HTML presentation patterns where MudBlazor provides an equivalent.
- Centralize the brand palette, typography, component defaults, spacing, and semantic colors in a `MudTheme`.
- Make the existing application work coherently on desktop, tablet, and mobile.
- Preserve the existing authentication, registration, onboarding, and logout behavior.

## Component Architecture

`MainLayout` becomes the single authenticated shell. It owns the theme and MudBlazor providers, an accessible responsive drawer, a top bar, company context, and the page content area.

`AuthLayout` becomes the single unauthenticated and onboarding shell. It owns the centered responsive surface and canonical `/logos/logo.png` presentation. Authentication pages only provide their heading, contextual text, form fields, and actions.

Reusable presentational components will be introduced only where the existing pages share a concrete pattern:

- `PageHeader` for a title, concise description, and optional primary action.
- `EmptyState` for helpful empty or unavailable page states.

Existing Brazilian document, phone, postal code, and password inputs will be implemented with MudBlazor fields while preserving native form submission names, formatting behavior, accessibility labels, and input attributes required by their endpoints.

## Visual System

The application uses the specified blue palette: primary `#0969F0`, hover `#0758CC`, accent `#08BDF2`, navy `#071D49`, and the defined neutral and semantic colors. `Inter` is loaded as the primary font with appropriate fallbacks. The theme configures these values, typography, defaults, component radii, and outlined input appearance; pages must not repeat color literals.

The layout uses an 8px-based rhythm and a maximum content width of 1440px. Pages use a light application background, white bordered surfaces, restrained 12px radii, and subtle shadows only when necessary.

## Screen Changes

### Authentication and Onboarding

Login, registration, and company onboarding share the canonical logo, centered card hierarchy, outlined MudBlazor fields, clear validation/error feedback, and full-width primary actions. Company onboarding remains a native multipart form to preserve file upload endpoint behavior, while its visible controls use MudBlazor where supported. The selection of state refreshes city options without changing endpoint payload names.

### Authenticated Application

The shell uses the VoltaMarcada logo, a responsive drawer, clear active navigation, compact company context, and a MudBlazor-compatible logout action. The dashboard gains a consistent page header and a helpful Phase Two operational empty state, rather than a placeholder card.

### Fallback and Transitional Screens

Not-found, error, and redirect screens gain localized, accessible status surfaces and clear recovery actions. The reconnect modal will remain framework-owned but will receive localized, branded text only if it can be done without replacing the framework behavior.

## Responsive and Accessibility Behavior

The drawer responds at the medium breakpoint. Main content uses 24px desktop padding and reduced mobile padding. Forms become single-column on small screens. All icon-only controls retain accessible labels and tooltips. Labels remain visible, errors are contextual, focus indicators use the theme color, and status content uses text in addition to color.

## Error Handling and Feedback

Current endpoint query-string errors are presented in `MudAlert` components, without exposing internal errors. Form submission remains server-owned; no business logic moves into Razor. Loading feedback is added only where the current interactive flow has an asynchronous operation, avoiding artificial client-side submission states for native POST forms.

## Validation

Validation is by static review because the project instructions prohibit builds, tests, and application launches unless explicitly requested. The review will verify component markup, endpoint field names, form attributes, theme integration, localized UI text, routing, accessibility attributes, and removal of obsolete Bootstrap styling.
