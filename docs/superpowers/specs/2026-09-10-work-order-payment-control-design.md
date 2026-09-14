# Work Order Payment Control Design

## Purpose

Phase 12 adds payment control to work order completion. When a company enables payment control, every completed work order with a positive total has an explicit financial outcome: a charge with installments or a documented no-charge exception.

The phase also delivers an owner-only daily Cash page. It provides reliable received and receivable values without introducing a generic ERP ledger or payment gateway.

## Scope

Included:

- Company payment-control and Pix-key settings.
- Work-order charges, split installments, manual confirmation, and no-charge exceptions.
- Pix BR Code generation, copy, QR display, and WhatsApp sharing.
- Payment step embedded in the existing work-order completion flow.
- Owner-only daily Cash page, daily navigation, installment editing/deletion, and audit history.
- Server-side authorization, tenant isolation, and integration coverage.

Excluded:

- Payment-gateway integration, automatic reconciliation, refunds, bank feeds, and a general accounting ledger.
- A period filter on Cash. Cash is explicitly a daily view.

## Domain Model

### Company

`Company` receives these owner-managed properties:

- `PaymentControlEnabled`
- `PixKey`
- `PixKeyType`: `Cpf`, `Cnpj`, `Email`, `Phone`, or `Random`

The company trade name and configured city provide the merchant name and city required by the existing BR Code Pix generator. A Pix installment can be recorded without a configured key, but cannot present a QR Code or copy-and-paste payload.

### Work Order

`WorkOrder` receives a no-charge audit record:

- `NoChargeReason`
- `NoChargeRecordedAtUtc`
- `NoChargeRecordedByUserId`

A positive-total work order can be completed with payment control enabled only when either a valid charge covers its total or a valid no-charge reason covers the total.

### Work Order Charge

`WorkOrderCharge` is a company-scoped entity with one record per work order. It captures the charge lifecycle and contains installments. Its total must match the work-order total after discounts.

`WorkOrderChargeInstallment` is company-scoped and records:

- `Amount`
- `PaymentMethod`: `Pix`, `Cash`, `DebitCard`, `CreditCard`, `BankTransfer`, or `Other`
- `OtherPaymentMethodDescription`, mandatory only for `Other`
- `Status`: `Pending` or `Paid`
- `ConfirmationType`: `Manual` when paid
- `PaidAtUtc` and `ConfirmedByUserId` when paid
- Pix payload data when applicable

An installment is independently confirmed. A split payment may therefore contain paid and pending amounts simultaneously.

`WorkOrderChargeInstallmentHistory` is an immutable audit record for updates and deletions. It preserves the prior values and status, mutation kind, required reason, acting user, and UTC timestamp. Editing or deleting a paid installment is allowed only to an Owner and creates history.

## Completion Flow

The existing execution panel keeps the maintenance recommendation step. After it validates, payment control behaves as follows:

1. With control disabled or an order total of zero, completion remains unchanged.
2. With control enabled and a positive total, the application opens the in-context payment dialog.
3. The operator adds installments. Their sum must exactly equal the work-order total.
4. Selecting Pix and entering a valid installment amount immediately displays its QR Code, copy-and-paste payload, copy action, and WhatsApp share action in the same dialog. The dialog remains open for edits and other installments.
5. Alternatively, the operator selects no charge and supplies a required reason.
6. The server validates the result and atomically persists the completed work order, optional maintenance recommendation, charge/no-charge record, and work-order history.

Generating, showing, copying, or sharing Pix never confirms a payment. New installments are `Pending`, even for cash and cards. Only an Owner may manually change an installment to `Paid`.

## Authorization and Consistency

Employees may create the charge, choose methods, display/copy/share Pix, and finish the completion flow with pending installments. They cannot access Cash or confirm, edit, or delete installments.

Owners may perform all employee actions plus confirm, edit, and delete installments. Every operation resolves the authenticated company and filters by `CompanyId`; route, form, and hidden fields never determine tenant ownership.

After a completed work order has a charge, an Owner may modify its installments only in a valid final state: active installments must still sum to the work-order total, or an explicit no-charge reason must account for the difference. A deletion is soft/audited so financial history remains reviewable. This prevents a closed order from silently becoming financially unaccounted for.

## Cash Page

The owner-only `Financeiro / Caixa` route is a daily operational page. It has a selected business date with previous-day, next-day, and date-picker navigation; it intentionally has no period filter. Until the product adds a configurable company timezone, daily grouping uses the initial-market timezone, `America/Sao_Paulo`.

For the selected date it shows:

- `Received`: sum of paid installments whose `PaidAtUtc` falls on the selected business date.
- `Receivable`: sum of pending installments from charges created on the selected business date.
- `No charge`: work-order total covered by a no-charge record on the selected date.
- An installment list with work order, customer, payment method, amount, status, confirmation date, and available actions.

Owners can open the work order, confirm a pending installment, edit an installment amount/method, or delete it. Editing and deletion require a reason and are recorded in immutable installment history. Totals update from persisted data after each successful mutation.

## Application and Web Boundaries

Application use cases remain explicit:

- Get and update company payment settings.
- Get payment-completion context and generate Pix payload.
- Complete a work order with a payment decision.
- Get daily Cash.
- Confirm, edit, and delete a charge installment.

The current `CompleteWorkOrderUseCase` becomes the single transactional enforcement point for completion. Razor components only collect inputs and render results. The Infrastructure project owns EF Core configurations, migration, query implementation, and the Pix payload adapter already used by the application.

## Errors

Expected business errors use operation results: payment control requires a charge, installment total mismatch, invalid amount/method, missing Other description, missing no-charge reason, unavailable Pix key, already paid installment, invalid closed-order charge state, and forbidden action.

## Verification

Integration tests cover:

- Payment control on/off and zero-total work orders.
- Full and split charges, including Pix payload amount per Pix installment.
- No-charge validation.
- Atomic work-order completion with a maintenance recommendation and payment decision.
- Employee and Owner authorization boundaries.
- Manual installment confirmation and daily Cash totals.
- Edit/delete audit history and recomposition/no-charge enforcement.
- Cross-company isolation for every charge and Cash operation.
