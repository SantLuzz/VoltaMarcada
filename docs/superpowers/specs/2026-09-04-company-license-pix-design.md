# Company License and PIX Design

## Purpose

Phase 11 introduces a company-scoped license lifecycle, manual PIX renewal
payments, and platform administration. It enables initial commercial operation
without a payment gateway while preserving tenant isolation, auditability, and
safe access restrictions.

## Scope and delivery order

The work is divided into three independently releasable increments:

1. License foundation and access policy.
2. PIX renewal and payment-proof submission.
3. Platform administration and overdue-license notifications.

No payment gateway, automatic PIX settlement, bank reconciliation, or user
editable platform PIX credentials are included.

## Configuration

The `LicenseBilling` configuration section is supplied by environment-specific
configuration or secrets. It contains the monthly price, trial length,
trial-tolerance length, paid-license read-only threshold, paid-license access
termination threshold, PIX key, beneficiary name, and beneficiary city.

The application validates this configuration at startup. The PIX key and
beneficiary data are never stored in the database or exposed through an
administrative editing screen. The application produces a static EMV/BR Code
payload and QR code locally from these values and the configured monthly price.

## Domain model

`CompanyLicense` is a separate company-scoped entity. A license is created
atomically when its company is created. It stores the company identifier,
trial start and end timestamps, paid-until timestamp when applicable, and the
current lifecycle state. Its behavior determines the current access mode from
the configured lifecycle thresholds and an injected current time.

Trial rules are:

- The trial is valid for 14 days from company creation.
- A 3-day tolerance permits normal operations after the trial.
- After tolerance, the owner has read-only access and can regularize the
  license; employees see the blocked-account screen.
- On the tenth day after trial end, operational access is terminated for both
  roles, with the owner sent to regularization and employees remaining blocked.

Paid-license rules are:

- A valid paid license permits normal operations.
- From the first overdue day through the seventh overdue day, users receive an
  in-product warning and the owner receives a daily e-mail.
- After seven overdue days, the owner is read-only and employees are blocked.
- After twenty overdue days, operational access is terminated.

`LicensePayment` records one manual renewal attempt. It stores the company,
amount, proof file key and content type, submitted timestamp, current status,
decision reason, decision timestamp, and deciding platform-administrator user
identifier. Statuses are `PendingReview`, `Approved`, and `Rejected`. The
database enforces at most one pending payment per company. Rejected payments
remain auditable and allow another submission.

Approving a payment renews the company license in one transaction. The new
paid-until date is one calendar month after the later of the current paid-until
date and approval time, preserving prepaid days.

`LicenseOverdueNotification` stores a license, owner user, and local calendar
date of a successful daily warning e-mail. A uniqueness constraint prevents
duplicate successful notices for the same owner and day.

## Authorization and access control

All authenticated internal write endpoints use a central license-write
authorization requirement. It resolves the company from authenticated
membership, never from browser input. The requirement permits writes only when
the license has normal operational access. It rejects requests before use-case
execution in the server.

Read behavior is resolved by a central license-access service. Owners whose
license has restricted access may view existing tenant data and the renewal
page, but cannot mutate data. Employees whose license is not operational are
directed to a blocked-account page. At terminated access, owners are directed
to regularization and employees remain blocked.

Platform administration is isolated behind a dedicated policy requiring only
the existing `PlatformAdministrator` role. It does not resolve or use a
company context.

## PIX and proof flow

The owner sees a renewal page whenever the license is near expiry, read-only,
or terminated. The page shows the configured monthly amount, a QR code, and a
copy-and-paste PIX payload. A payment submission creates a pending
`LicensePayment` and uploads the proof through the existing `IFileStorage`
abstraction.

Accepted proof formats are PDF, JPEG, PNG, and WebP. The application validates
content type, extension, and a configured maximum size before upload. Files
are stored with the exact structure:

`pagamentos/{companyId}/{paymentId}.{extension}`

Files are never exposed by public URL. An authenticated download endpoint
checks that the caller is the owner of the payment company or a platform
administrator before retrieving bytes from storage.

## Platform administration and notifications

The platform-administrator dashboard lists company, owner identity and e-mail,
license expiry, payment amount/date, and payment-review status. It supports
filtering pending payments and date ranges. The administrator may securely
view a proof, approve it, or reject it with a required reason. Every decision
is retained with actor and timestamp.

A hosted service runs daily. It finds paid licenses currently overdue but not
yet at terminated access and sends the existing e-mail abstraction a warning
to each company owner. It records successful sends to avoid duplication across
restarts. Failed sends are logged and retried on the next run; they do not
change license state.

## Error handling and concurrency

Expected outcomes use the existing `OperationResult` pattern: no company
license, restricted access, duplicate pending payment, invalid proof, payment
not found, and invalid review state. The final payment approval transaction
rechecks that the payment is still pending and that the license belongs to the
payment company. Database constraints protect pending-payment and daily-notice
uniqueness.

All timestamps are UTC. Trial and paid lifecycle calculations use `TimeProvider`.
All tenant queries filter by both company ownership and entity identifier when
applicable.

## Testing

xUnit tests cover license lifecycle boundaries, renewal date preservation,
single-pending-payment behavior, proof authorization, payment approval state
transitions, and notification de-duplication. Tests use deterministic
`TimeProvider` instances and do not depend on the machine clock.
