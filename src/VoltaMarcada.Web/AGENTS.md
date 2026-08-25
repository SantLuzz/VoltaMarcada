# VoltaMarcada Frontend UI / UX Design System

The VoltaMarcada frontend must look and behave like a modern, professional, clean, and consistent SaaS application.

The UI must prioritize:

- clarity
- usability
- visual hierarchy
- consistency
- responsiveness
- accessibility
- low cognitive load
- fast task completion

VoltaMarcada is primarily used by service technicians and small service businesses.

The interface must feel simple, organized, trustworthy, and professional even when the underlying workflow is complex.

Frontend work is not complete simply because the functionality works.

The visual quality, consistency, responsiveness, component reuse, loading states, validation states, and overall UX are part of the implementation requirements.

---

# UI Refactoring Rule

Whenever modifying an existing frontend page, first inspect its current visual structure.

If the page does not comply with the VoltaMarcada UI rules defined in this document, refactor the relevant layout, spacing, components, form controls, and visual hierarchy to conform to the design system before completing the task.

Existing bad UI is not a reason to propagate the same pattern.

Do not preserve inconsistent styling just because it already exists.

Every new page and every modified page must follow the same:

- visual language
- spacing system
- component usage
- color palette
- typography
- responsive behavior
- interaction patterns

When working on an existing page, improve obviously inconsistent UI when it is reasonably related to the requested change.

Do not create page-specific visual patterns unless there is a strong UX reason.

---

# MudBlazor is Mandatory

MudBlazor is the default and mandatory UI component library for the VoltaMarcada frontend.

Whenever MudBlazor provides a component for a UI element, use the MudBlazor component.

Never manually recreate a UI or form control that already exists in MudBlazor.

All user-editable fields must use an appropriate MudBlazor input component.

Raw HTML form fields are forbidden by default.

Before implementing any form field or interactive UI element, check whether an equivalent MudBlazor component already exists.

Custom input components are only allowed when MudBlazor cannot satisfy the requirement.

Even custom reusable VoltaMarcada input components should internally use MudBlazor components whenever possible.

## Component Mapping

Prefer:

- Text input -> `MudTextField`
- Email input -> `MudTextField`
- Password input -> `MudTextField`
- Multiline text -> `MudTextField` with multiline configuration
- Numeric input -> `MudNumericField`
- Select -> `MudSelect`
- Autocomplete -> `MudAutocomplete`
- Checkbox -> `MudCheckBox`
- Radio -> `MudRadioGroup`
- Date input -> `MudDatePicker`
- Time input -> `MudTimePicker`
- Date range -> MudBlazor date range components
- Switch -> `MudSwitch`
- Button -> `MudButton`
- Icon button -> `MudIconButton`
- Table -> `MudTable` or `MudDataGrid`
- Dialog -> `MudDialog`
- Snackbar -> MudBlazor Snackbar
- Tabs -> `MudTabs`
- Menu -> `MudMenu`
- Chip -> `MudChip`
- Tooltip -> `MudTooltip`
- Pagination -> `MudPagination`
- Loading -> MudBlazor progress components
- Icons -> MudBlazor icons
- Navigation -> MudBlazor navigation components when appropriate

Do not use raw HTML controls such as:

`<input>`

`<select>`

`<textarea>`

`<button>`

unless there is a documented technical reason and MudBlazor cannot satisfy the requirement.

Do not introduce another UI component library for functionality already available in MudBlazor.

---

# VoltaMarcada Official Logo

The official VoltaMarcada logo asset is located at:

`src/VoltaMarcada.Web/wwwroot/logos/logo.png`

This is the canonical VoltaMarcada logo and must be reused throughout the application.

Because the file is located under `wwwroot`, Blazor components should reference it using:

`/logos/logo.png`

Do not duplicate the logo into another folder.

Do not create alternative versions of the logo unless explicitly requested.

Do not recreate the VoltaMarcada logo using:

- plain text
- CSS
- MudBlazor icons
- generic icons
- generated SVGs
- alternative images

when the official asset can be used.

Always preserve the original aspect ratio.

Never stretch or distort the logo.

---

# Visual Identity

The VoltaMarcada visual identity is based on blue tones derived from the official logo.

## Primary Colors

Primary:

`#0969F0`

Primary Hover:

`#0758CC`

Accent:

`#08BDF2`

Navy:

`#071D49`

## Neutral Colors

Application Background:

`#F6F8FC`

Surface / Cards:

`#FFFFFF`

Border:

`#E2E8F0`

Primary Text:

`#172033`

Secondary Text:

`#64748B`

## Semantic Colors

Success:

`#16A34A`

Warning:

`#F59E0B`

Danger:

`#DC2626`

These colors must be centralized in the application theme.

Prefer configuring them through the VoltaMarcada `MudTheme`.

Do not hardcode these colors repeatedly across individual pages.

Do not introduce random colors without a strong semantic or design reason.

---

# MudBlazor Theme

VoltaMarcada should maintain a centralized MudBlazor theme.

The theme should be the primary source of visual configuration for:

- primary color
- secondary/accent color
- background
- surfaces
- typography
- border appearance
- semantic colors
- default component appearance where supported

Prefer theme-level customization over page-level CSS.

Do not solve global design problems with duplicated page-specific CSS.

---

# Typography

Use `Inter` as the primary application font whenever available.

Typography must remain consistent throughout the application.

## Page Title

- Size: approximately 28px
- Weight: 700
- Primary text color

## Section Title

- Size: approximately 20px
- Weight: 600

## Card Title

- Size: approximately 16px
- Weight: 600

## Body

- Size: 14px or 16px
- Weight: 400

## Secondary Text

- Size: approximately 14px
- Secondary text color

Avoid unnecessary typography variations.

Do not create arbitrary font sizes for individual pages.

Use MudBlazor typography components or shared typography rules whenever appropriate.

---

# Spacing System

VoltaMarcada must use a consistent spacing system.

Prefer an 8px-based spacing system.

Allowed spacing values should generally come from:

- 4px
- 8px
- 12px
- 16px
- 24px
- 32px
- 48px

Avoid arbitrary spacing values unless strictly necessary.

The most common spacing values should be:

- 8px for small internal gaps
- 16px between related elements
- 24px between groups
- 32px between major page sections

Whitespace is intentional.

Do not compress screens simply to fit more information.

Pages should feel comfortable and visually balanced.

---

# Application Layout

Authenticated desktop application pages should use a consistent application shell.

The standard structure should normally be:

- Sidebar
- Topbar
- Page Header
- Main Content

Main content should use:

- comfortable horizontal padding
- comfortable vertical padding
- consistent maximum width where appropriate

Recommended desktop page padding:

`24px` to `32px`

Recommended maximum content width:

approximately `1440px`

Never place page content directly against browser edges.

Avoid large unused areas when content could be presented more effectively.

---

# Page Header

Every management page should have a consistent page header.

The page header should normally contain:

- Page title
- Short description
- Primary action aligned to the right when applicable

Example:

```text
Clientes                              + Novo cliente

Gerencie seus clientes e equipamentos.
```

On mobile, actions may move below the title.

Do not create a completely different header structure for every page.

Prefer creating or reusing a shared `PageHeader` component.

---

# Management List and Registration Flow

Management modules must use a two-screen flow:

1. A list screen is the module entry point and presents search, filters, records, and the primary create action.
2. A separate registration screen handles creating and editing a single record.

Use routes and navigation that reflect this pattern. For example:

```text
/clientes                 -> customer list
/clientes/registro        -> create customer
/clientes/registro?id=... -> edit customer
```

The registration screen must provide a clear Cancel action that returns to the list. After a successful create or update, return to the list and show a success message.

Do not combine a full management list and a full registration form on the same page unless a concrete workflow requires it.

---

# Cards

Cards must use a consistent visual style.

Default card appearance:

- white background
- subtle border
- approximately 12px border radius
- 16px to 24px internal padding
- subtle shadow only when useful
- comfortable spacing from surrounding elements

Avoid excessive shadows.

Avoid overly rounded cards.

Avoid unnecessary gradients.

Avoid nesting multiple cards when visual grouping can be achieved more simply.

Cards should represent meaningful content groups.

---

# MudBlazor Forms Rule

Never manually recreate a form control that already exists in MudBlazor.

All user-editable fields must use an appropriate MudBlazor input component.

Raw HTML form fields are forbidden by default.

All forms throughout VoltaMarcada must have a consistent visual appearance.

---

# Form Fields

All form fields must use a consistent visual style.

Use MudBlazor outlined fields by default.

Preferred configuration:

- `Variant="Variant.Outlined"`
- clear and concise `Label`
- consistent width
- comfortable input height
- proper validation messages
- helper text only when useful
- appropriate input type
- appropriate icons only when they improve comprehension

Use:

`FullWidth="true"`

whenever the field belongs to a form layout where width consistency improves usability.

Use `Margin="Margin.Dense"` only when the resulting input still has comfortable height and readability.

Inputs must never look excessively small or compressed.

Form controls must have a professional SaaS appearance.

Do not manually style every field independently.

If the same configuration is repeated across the application, prefer:

1. MudTheme configuration
2. shared VoltaMarcada component
3. shared CSS class

instead of duplicated styling.

---

# Input Width and Height

Inputs must have enough visual presence to be easy to read and interact with.

Do not create narrow inputs unless the expected value is naturally small.

For example, fields such as:

- Name
- Email
- Company Name
- Phone
- Address

should normally use the available width.

Fields such as:

- State
- Number
- Short codes

may use smaller widths when appropriate.

All inputs in the same visual group should have consistent heights.

Do not mix visibly different input heights without reason.

---

# Form Layout

Forms must have clear visual rhythm and grouping.

Recommended spacing:

- 16px between related fields
- 24px between form sections
- 24px to 32px before form actions

Avoid compressed forms.

Avoid placing fields too close together.

Group fields based on meaning.

Example:

```text
Company Information

Company Name
Trade Name
CNPJ

Contact Information

Phone
Email

Address

ZIP Code
Street
Number
City
State
```

Do not create one giant unstructured group of fields.

---

# Form Columns

On desktop, related fields may use a two-column layout when appropriate.

Example:

```text
First Name          Last Name

Phone               Email

City                State
```

Fields requiring more space should occupy the entire row.

Examples:

- Company Name
- Address
- Description
- Notes

On mobile, forms must collapse to a single-column layout.

Do not force multiple columns on small screens.

Use MudBlazor grid/layout components whenever appropriate.

---

# Authentication and Onboarding Pages

The following pages must share the same visual identity:

- Login
- Create Account
- Create Company
- Forgot Password
- Reset Password
- Email Confirmation
- Any future authentication page
- Any future onboarding page

These screens must look like parts of the same VoltaMarcada onboarding flow.

Do not design these screens independently.

---

# Authentication Page Layout

Authentication and onboarding pages must use a centered card layout.

The page background should use the standard VoltaMarcada application background:

`#F6F8FC`

The page must feel:

- clean
- simple
- modern
- professional
- trustworthy

Avoid visually noisy backgrounds.

Avoid excessive gradients.

Avoid unnecessary decorative elements.

The form must remain the primary visual focus.

---

# Authentication Card

Authentication cards must:

- be centered horizontally
- preferably be centered vertically when appropriate
- have comfortable internal padding
- have a reasonable maximum width
- use a white surface
- use a subtle border
- use a subtle shadow
- use rounded corners
- be fully responsive

Recommended width:

`420px` to `480px`

Recommended padding:

`24px` to `32px`

The Create Company page may use a slightly wider card if necessary.

Do not create an unnecessarily wide Login or Create Account form.

On mobile, the card must adapt to the available width while preserving comfortable horizontal margins.

---

# Authentication Logo

The official VoltaMarcada logo must appear at the top of:

- Login
- Create Account
- Create Company
- Forgot Password
- Reset Password
- Email Confirmation
- Any future authentication screen
- Any future onboarding screen

Always use:

`/logos/logo.png`

The source asset is:

`src/VoltaMarcada.Web/wwwroot/logos/logo.png`

The logo must:

- be horizontally centered
- appear at the top of the card
- preserve its original aspect ratio
- have a consistent visual size
- have comfortable whitespace around it
- have enough spacing below it before the title
- never be stretched
- never be distorted

Do not replace it with:

- text-only branding
- `MudIcon`
- generic service icons
- another image

Prefer creating a shared authentication layout or reusable branding component so every authentication screen uses exactly the same logo positioning and sizing.

---

# Authentication Shared Layout

Prefer implementing a shared layout/component for authentication and onboarding pages.

For example:

`AuthLayout`

or an equivalent existing project convention.

The shared authentication layout should be responsible for:

- background
- card positioning
- card width
- card appearance
- logo
- common spacing
- responsive behavior

Individual authentication pages should primarily provide their specific:

- title
- description
- fields
- actions

Do not duplicate the entire authentication card markup across every authentication page when it can be shared.

---

# Authentication Content Hierarchy

Authentication cards should normally follow this structure:

```text
VoltaMarcada Logo

Page Title

Short Supporting Description

Form Fields

Primary Action

Secondary Navigation
```

Example:

```text
        [ VoltaMarcada Logo ]

        Create your account

Start organizing your services and customers.

[ Name                              ]

[ Email                             ]

[ Password                          ]

[ Confirm password                  ]

[        Create account             ]

Already have an account? Sign in
```

Maintain comfortable vertical spacing between each section.

---

# Login Page

The Login page must remain simple.

The form may contain:

- Email
- Password
- Remember Me, if supported
- Forgot Password action
- Primary Login button
- Create Account link

Do not overpopulate the login screen.

Use `MudTextField` for Email.

Use `MudTextField` for Password.

Both must use the outlined variant.

The primary Login button should normally:

- use `MudButton`
- use the primary VoltaMarcada color
- use the filled variant
- be full width
- have comfortable height

---

# Password Fields

Password fields must use `MudTextField`.

Use the appropriate password input type.

When useful, include a MudBlazor visibility icon using `MudIconButton` or the appropriate MudBlazor adornment configuration to allow users to show or hide the password.

Do not create custom password inputs using raw HTML.

Password fields must follow the same:

- dimensions
- outlined variant
- spacing
- typography

as other inputs.

---

# Create Account Page

The Create Account page must use the same visual structure as Login.

Use:

- official VoltaMarcada logo
- centered card
- clear page title
- short description
- outlined MudBlazor fields
- comfortable spacing
- full-width primary action
- secondary navigation back to Login

If account creation requires too many fields, split the process into multiple onboarding steps.

Do not create excessively long registration forms.

---

# Create Company Page

The Create Company page must visually continue the onboarding experience.

Use:

- official VoltaMarcada logo
- consistent card
- consistent typography
- outlined MudBlazor fields
- logical field grouping
- responsive layout

Possible groups:

- Company Information
- Contact Information
- Business Information
- Address

If many fields are required, the card may be wider than the Login card.

A step-based onboarding flow is preferred over an extremely long single form.

Do not remove the VoltaMarcada branding just because the Create Company form is larger.

---

# Buttons

Use `MudButton`.

Do not use raw HTML buttons when MudBlazor can satisfy the requirement.

## Primary Actions

Primary actions should normally use:

- Filled variant
- VoltaMarcada primary color
- clear action text
- comfortable height

Authentication primary buttons should normally be full width.

Examples:

- Sign in
- Create account
- Create company
- Continue
- Save customer
- Create service order
- Send quote

Avoid generic labels when a clearer action exists.

Avoid:

- Submit
- OK
- Confirm

when the actual action can be described explicitly.

---

# Button Hierarchy

Primary action:

Filled button using the VoltaMarcada primary color.

Secondary action:

Outlined or neutral button.

Destructive action:

Danger/error styling.

Avoid displaying multiple competing primary buttons in the same section.

Users should immediately understand the main action available on the screen.

---

# Tables and Data Grids

Use:

- `MudTable`

or

- `MudDataGrid`

Tables must have:

- comfortable row height
- clear headers
- subtle separators
- hover state where useful
- empty state
- loading state
- responsive behavior
- consistent action placement

Avoid putting many visible action buttons into every table row.

Prefer an action menu using `MudMenu` or `MudIconButton` when multiple secondary actions exist.

---

# Filters and Search

Search fields must use MudBlazor components.

Prefer `MudTextField` with an appropriate search icon.

Filters should use appropriate MudBlazor components such as:

- `MudSelect`
- `MudAutocomplete`
- `MudDatePicker`
- `MudMenu`

Do not build custom filter controls when MudBlazor already provides the required functionality.

Keep filter interfaces visually simple.

---

# Status

Never communicate status using color alone.

Use textual status components together with semantic colors.

Prefer MudBlazor components such as:

`MudChip`

Examples:

- Scheduled
- In Progress
- Completed
- Cancelled
- Pending Approval
- Quote Sent
- Awaiting Customer
- Maintenance Due

Status colors must be semantically consistent throughout the application.

The same status must not appear with different colors on different pages.

---

# Empty States

Never leave an empty table, blank card, or unexplained empty screen.

Provide:

- short explanation
- optional MudBlazor icon
- clear CTA when appropriate

Example:

```text
No customers yet

Add your first customer to start creating
service orders.

[ Add customer ]
```

Empty states should help users understand what happened and what they can do next.

---

# Loading States

Every asynchronous page or operation must provide appropriate feedback.

Use MudBlazor loading components.

Do not leave users wondering whether an action is running.

Buttons that trigger asynchronous operations should:

- display loading feedback when appropriate
- prevent duplicate submissions
- be disabled while processing when necessary

Avoid blocking the entire screen when only one section is loading.

---

# Validation

Use MudBlazor validation integration whenever possible.

Validation messages must appear close to the related field.

Do not display raw backend exception messages to users.

Do not use JavaScript alerts for validation.

Forms must provide:

- loading state
- disabled state during submission
- error feedback
- success feedback when appropriate

Validation must be clear, concise, and actionable.

---

# Form Actions

Form actions must be visually separated from form fields.

Recommended behavior:

- primary action aligned to the right on desktop when appropriate
- primary action full width on authentication pages
- secondary actions adjacent but visually less prominent
- cancel action must not look like a primary action
- destructive actions must use danger styling

Use clear action labels.

Prefer:

- Save Client
- Create Company
- Update Service Order
- Cancel Changes

Avoid generic labels such as:

- Submit
- OK
- Save

when a more specific action label is available.

---

# Buttons

Use MudButton for standard actions.

Use MudIconButton for compact icon-only actions.

Buttons must have clear hierarchy.

## Primary Button

Use for the main action in a section.

Primary buttons should normally:

- use the VoltaMarcada primary color
- use a filled variant
- have clear action text
- be visually prominent
- have an appropriate loading state when asynchronous
- avoid being duplicated unnecessarily in the same section

## Secondary Button

Use for supporting actions.

Secondary buttons may use:

- outlined variant
- text variant
- neutral styling

## Destructive Button

Use danger styling only for destructive actions, such as:

- Delete Client
- Cancel Service Order
- Remove User
- Disable Account

Always make destructive actions explicit.

Do not use a destructive button for a harmless action.

Do not show several competing primary buttons in the same visual area.

---

# Icon Buttons

Use MudIconButton for compact contextual actions.

Icon buttons must:

- use a recognizable icon
- include a MudTooltip when the icon is not universally obvious
- have an accessible label
- have a sufficiently large interactive area
- not be the only way to discover a critical action when text is clearer

Prefer an action menu for a dense table row rather than many visible icon buttons.

Do not use decorative icons as controls.

---

# Tables and Data Grids

Use MudTable or MudDataGrid for tabular data.

Tables must have:

- clear column headers
- comfortable row height
- readable text
- subtle separators
- hover feedback when rows are interactive
- logical alignment for numeric and status columns
- responsive behavior
- loading state
- empty state
- error state when appropriate
- pagination or virtualized loading when the data volume requires it

Every `MudTable` and `MudDataGrid` must use the shared `servicesync-data-grid` styling to remain within its container without horizontal scrolling. Render textual data cells with `GridTextCell`, which truncates overflow and exposes the complete value in a tooltip. Keep explicit tooltips on icon-only actions; do not truncate status chips or action controls.

Actions in a table should normally use an action menu instead of several buttons.

Do not create a table that becomes unusable on small screens.

When a full table does not work well on mobile, use a responsive summary/card presentation or allow users to access essential details through a clear alternative.

---

# Search and Filters

Search and filtering controls should be grouped consistently.

A typical data-management page may contain:

- search field
- filters
- date range when relevant
- clear filters action
- primary create action

Place search and filters near the data they control.

Avoid scattering filter controls across the page.

Use MudBlazor components for search and filters whenever possible.

Filter values must remain understandable and easy to reset.

---

# Status Indicators

Never communicate status using color alone.

Use textual labels with appropriate MudBlazor components such as MudChip, badges, or equivalent shared VoltaMarcada components.

Examples:

- Scheduled
- In Progress
- Completed
- Cancelled
- Awaiting Approval
- Overdue

Status indicators must:

- have clear text
- use a semantic color
- maintain accessible contrast
- be consistent across the application

Do not use arbitrary status colors from page to page.

---

# Empty States

Never leave an empty table, blank card, or empty page without guidance.

An empty state should normally contain:

- short explanation
- optional appropriate icon
- clear next action when useful

Example:

```text
No clients registered yet

Create your first client to start managing services and equipment.

[ Add Client ]
```

Empty states should help users understand what the screen is for and what to do next.

Do not use empty states as a substitute for handling loading or error states.

---

# Loading States

Every asynchronous screen or section must provide appropriate loading feedback.

Use MudBlazor progress components whenever possible.

Examples:

- page loading indicator for an initial page load
- skeleton or content placeholder when useful
- button loading state during form submission
- inline progress indicator for a section refresh
- table loading state during data retrieval

Do not leave users wondering whether an action is running.

Buttons that trigger asynchronous operations should:

- display loading feedback when appropriate
- prevent duplicate submissions
- be disabled while processing when necessary

Avoid blocking the entire screen when only one section is loading.

---

# Error States

Error feedback must be clear, useful, and respectful.

Use:

- inline field validation for field-level errors
- contextual error message for section-level errors
- MudBlazor Snackbar or an equivalent shared notification for transient errors
- retry action when the user can safely retry

Do not expose:

- raw exceptions
- stack traces
- technical backend messages
- database errors
- unlocalized internal error codes

Error messages should explain the next useful action whenever possible.

---

# Success Feedback

Give users clear confirmation after meaningful actions.

Examples:

- Client created successfully.
- Service order updated successfully.
- Company information saved successfully.
- Password reset link sent.

Use a MudBlazor Snackbar or equivalent shared feedback pattern for transient success messages.

Do not overuse notifications for actions whose immediate UI update is already obvious.

---

# Dialogs and Confirmations

Use MudDialog for dialogs and confirmations.

Dialogs must be used only when they improve the workflow.

Use confirmation dialogs for destructive or irreversible actions.

A confirmation dialog should clearly communicate:

- what will happen
- which item is affected when relevant
- whether the action can be undone
- the primary confirmation action
- the cancel action

Do not use dialogs for simple forms that can be comfortably displayed in the normal page flow.

Do not create custom modal behavior when MudDialog satisfies the requirement.

---

# Navigation

Authenticated pages should use consistent navigation patterns.

Use MudBlazor navigation components when appropriate.

Navigation must:

- make the current location clear
- group related areas logically
- avoid overwhelming the user with too many top-level options
- remain usable on mobile
- preserve familiar labels and icons across the application

Do not introduce a new navigation pattern for an individual feature without a strong UX reason.

---

# Responsive Behavior

Every page must support desktop, tablet, and mobile layouts.

## Desktop

Desktop pages should normally provide:

- sidebar when appropriate
- topbar when appropriate
- comfortable page padding
- usable content width
- multi-column forms only where beneficial
- clear hierarchy between page sections

## Tablet

Tablet layouts must preserve usability without relying on desktop-only width.

Consider:

- collapsing secondary actions
- reducing multi-column layouts
- adapting sidebars
- maintaining tap-friendly controls

## Mobile

Mobile layouts must:

- keep forms in a single column
- keep fields full width where appropriate
- keep buttons easy to tap
- preserve comfortable horizontal margins
- avoid horizontal scrolling
- allow primary actions to remain easy to find
- adapt tables or data-heavy views meaningfully

Do not assume a desktop layout will naturally become usable on mobile.

Test responsive behavior intentionally.

---

# Accessibility

The VoltaMarcada frontend must meet practical accessibility expectations.

Ensure:

- sufficient color contrast
- status is not communicated by color alone
- all interactive controls have accessible labels
- keyboard users can reach and use controls
- focus states are visible
- form fields have clear labels
- validation messages are associated with the relevant fields
- icons used as buttons have tooltips and accessible names
- touch targets are comfortable on mobile
- text remains readable at common zoom levels

Do not use placeholder text as the only label for a field.

Do not hide essential information behind hover-only interactions.

---

# Existing Screens

When modifying an existing screen, review the following before considering the work complete:

1. Is it using MudBlazor components where available?
2. Are raw HTML controls replaced when MudBlazor provides an equivalent?
3. Are fields using the outlined style by default?
4. Is spacing consistent with the VoltaMarcada spacing system?
5. Is the page responsive?
6. Does the page follow the VoltaMarcada visual hierarchy?
7. Are buttons consistent and clearly prioritized?
8. Are labels clear and concise?
9. Are loading, empty, error, validation, and success states handled as applicable?
10. Are existing shared components reused before creating new ones?
11. Does authentication or onboarding content use the shared identity and logo treatment where applicable?
12. Does the implementation preserve accessibility expectations?

If any answer is no, improve the screen as part of the task when reasonably related to the requested change.

---

# Do Not

Do not:

- mix multiple UI component libraries
- introduce Bootstrap form controls
- use raw HTML inputs when MudBlazor has an equivalent
- manually recreate MudBlazor controls
- create random CSS for each page
- hardcode theme colors repeatedly across pages
- use arbitrary spacing values
- create inconsistent field heights
- use filled inputs on one page and outlined inputs on another without reason
- use multiple visual styles for primary buttons
- place the VoltaMarcada logo differently across authentication pages
- duplicate UI components that already exist
- create desktop-only layouts
- sacrifice responsive behavior for desktop-only design
- display raw backend errors to users
- use JavaScript alerts for normal validation or feedback
- create visually noisy authentication pages
- add gradients, shadows, or decorative elements without a clear UX reason

---

# Implementation Priority

When implementing frontend UI, prioritize in this order:

1. Reuse existing VoltaMarcada shared components.
2. Use an existing MudBlazor component.
3. Compose multiple MudBlazor components.
4. Configure or extend the centralized MudTheme.
5. Add shared VoltaMarcada styling.
6. Create a custom reusable component only as a last resort.
7. Create page-specific styling only when a strong UX reason requires it.

The frontend must look like one cohesive product, not a collection of independently generated pages.

---

# Definition of Done

A frontend page or change is complete only when all applicable items below are satisfied:

- The requested functionality works.
- The page follows the VoltaMarcada UI / UX Design System.
- MudBlazor components are used whenever an appropriate component exists.
- No unnecessary raw HTML form controls were introduced.
- The official logo is used from /logos/logo.png on authentication and onboarding screens.
- Theme colors, typography, and spacing are consistent.
- Forms use outlined MudBlazor fields with clear labels and validation.
- Buttons have clear hierarchy, meaningful labels, and loading/disabled states where necessary.
- Loading, empty, validation, error, and success states are handled where applicable.
- The page works on desktop, tablet, and mobile without unintended horizontal scrolling.
- Accessibility expectations are met, including labels, focus, contrast, and non-color status indicators.
- Existing shared layouts and components are reused where appropriate.
- Page-specific styling was avoided unless it is justified by a real UX requirement.
- The final UI looks clean, professional, consistent, and ready for real VoltaMarcada users.

Functionality alone is not sufficient for frontend completion.

Visual quality, consistency, responsiveness, accessibility, feedback states, and component reuse are required parts of the work.
