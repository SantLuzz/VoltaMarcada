# License Payments and Manual PIX Renewal Design

## Objective

Phase 10 introduces one monthly VoltaMarcada license plan, manual PIX payment submission, manual payment approval, and license-aware access control. It lets a company owner renew through a PIX QR code and copy-and-paste payload, upload a mandatory proof of payment, and wait for a platform administrator to approve the renewal.

The feature must preserve tenant isolation and use explicit operation results for expected business conditions. It must not use exceptions to represent expired trials, overdue licenses, pending payment submissions, or denied write access.

## Configuration

The Web host owns an options section named `Licensing`. The existing `parameters.free_trial_days = 14` remains the configured source for the trial duration and is bound through the licensing options implementation.

The configuration contains:

- `FreeTrialDays`: `14`.
- `TrialOfferDays`: `3`.
- `TrialGraceDays`: `3`.
- `TrialAccessTerminationDays`: `10` days after the trial end.
- `OverdueWriteGraceDays`: `7` days after a paid license expires.
- `OverdueAccessTerminationDays`: `20` days after a paid license expires.
- `MonthlyAmount`: `49.90`.
- `PixKey`: the random PIX key supplied through configuration or a secret provider.
- `PixRecipientName`: `Service Sync app`.
- `PixRecipientCity`: `Chapadão do Sul`.
- `OverdueEmailHour`: `09:00` in `America/Sao_Paulo`.
- SMTP host, port, credentials, sender address, and sender display name.

PIX and SMTP credentials are configuration secrets. They are never committed with a real value, logged, emitted in an error message, or accepted from a browser request.

## Domain Model

### CompanyLicense

`CompanyLicense` is a tenant-owned entity with one record per `Company`. It is created in the same persistence transaction that creates a company and its owner membership.

It stores:

- `Id`
- `CompanyId`
- `TrialStartedAtUtc`
- `TrialEndsOn`
- `PaidThroughOn` (nullable)
- `LastOverdueNotificationOn` (nullable)
- `CreatedAtUtc`
- `UpdatedAtUtc`

Dates that express a business day use `DateOnly` in the configured company/default time zone. The clock is obtained from `TimeProvider`.

The entity exposes behavior to record a daily notice and to extend the paid period. A payment approval uses the latest of the current paid-through date and the local approval date as its base, then applies `AddMonths(1)`. This preserves remaining days for an early renewal.

### LicensePaymentSubmission

`LicensePaymentSubmission` is a tenant-owned entity representing a proof submitted by the company owner. It maintains the manual review audit trail and never directly changes the license without a platform-administrator approval.

It stores:

- `Id`
- `CompanyId`
- `Amount`
- `PixTransactionId`
- `ProofFileKey`
- `ProofContentType`
- `ProofFileName`
- `SubmittedAtUtc`
- `Status` (`Pending`, `Approved`, `Rejected`)
- `ReviewedByUserId` (nullable)
- `ReviewedAtUtc` (nullable)
- `RejectionReason` (nullable)
- `AppliedPaidThroughOn` (nullable)

At most one pending payment submission may exist for a company. A rejected payment can be followed by a new submission. Approval is idempotent: only a pending submission may be approved, and an approved submission cannot extend a license again.

## License State Evaluation

`ILicenseAccessEvaluator` belongs to Application and resolves a `CompanyLicenseAccess` result for the current company. The state is derived from persisted dates and the current local date; it is not an independently mutable status column.

The states are:

| Condition | Owner access | Employee access |
| --- | --- | --- |
| Trial active | Full access | Full access |
| Final three trial days | Full access and adherence prompt | Full access |
| Trial grace: first three days after trial end | Full access and renewal prompt | Full access |
| Trial restricted: after grace through the ninth day after trial end | Read-only and renewal access | Blocked-account page |
| Trial terminated: from the tenth day after trial end | Renewal page only | Blocked-account page |
| Paid license active | Full access | Full access |
| Paid overdue: first seven days after expiry | Full access with daily notice | Full access with daily notice |
| Paid restricted: from the eighth through the twentieth day after expiry | Read-only and renewal access | Blocked-account page |
| Paid terminated: from the twenty-first day after expiry | Renewal page only | Blocked-account page |

The trial end day is the first day after the 14-day trial interval. Trial grace therefore covers the first through third days after that end; the terminated state begins when the local date is greater than or equal to `TrialEndsOn.AddDays(10)`. A paid expiry date remains valid through its local calendar day. Paid write restriction begins when the local date is greater than `PaidThroughOn.AddDays(7)`, and paid termination begins when it is greater than `PaidThroughOn.AddDays(20)`.

Write access is enforced in Application use cases through a small authorization abstraction before mutation. UI controls are also disabled, but UI state is never the authorization mechanism. Read-only queries remain available to owners in restricted states. Platform administrators bypass company license access rules for the administrative dashboard only.

The Web layer redirects terminated owners to `/licenca/renovar` and employees to `/conta-bloqueada`. The blocked employee page only says that the account is blocked and instructs the employee to contact the company administrator. It reveals no PIX value, payment status, or company financial information.

## PIX and Payment Submission Flow

`GetLicenseRenewalPayment` returns a PIX BR Code payload generated from the server-side licensing configuration and a stable application transaction identifier. The value is fixed at `49.90` for the single available plan. The Razor page renders the QR code locally from that payload and provides a copy action.

`SubmitLicensePaymentProof` accepts the proof only from the current company owner. It validates the file before upload:

- JPEG, PNG, WebP, HEIC, or PDF;
- non-empty;
- maximum size defined in licensing options;
- one pending submission per company.

The file storage abstraction is extended to support both image and raw document resources. The proof storage object uses this key format:

```text
pagamentos/{companyId:N}/{submittedAtUtc:yyyyMMddHHmmssfff}.{extension}
```

The timestamp is the payment submission timestamp in UTC and prevents overwriting another proof sent on the same day. Proof resources use authenticated cloud delivery and are never exposed as a browser-facing storage URL. A platform administrator retrieves proof content only through an authorized server endpoint. If database persistence fails after upload, the uploaded object is deleted and the unexpected failure propagates.

Expected submission outcomes return `OperationResult`: invalid file, existing pending submission, no company owner authorization, or license not eligible for renewal. Unexpected cloud, database, or QR rendering errors remain unexpected failures.

## Platform Administration

`/admin/licencas` is a dedicated platform-administrator route and uses an administrative layout that does not resolve a current company. It shows a month-selectable grid with:

- company name and identifier;
- owner name and email;
- current license expiry;
- submission date;
- amount;
- payment status;
- reviewer and review timestamp when available;
- proof-view action;
- approve and reject actions for pending submissions.

Proof content is served only after platform-role authorization. Approving a submission calls one use case that atomically marks it approved, assigns the reviewer, and extends the license. Rejecting requires an optional bounded reason and does not alter the current license.

## Owner Experience

The company owner dashboard displays the current license/trial state and the number of remaining days. It shows:

- `Aderir` during the final three trial days;
- `Renovar licença` when the paid license has seven or fewer days remaining;
- an overdue notice while a paid license is overdue;
- a read-only notice and renewal action while the account is restricted.

The renewal page displays the fixed plan value, PIX QR code, copy-and-paste PIX field, mandatory proof upload, and the status of any pending payment submission. It uses Portuguese user-facing text. Internal identifiers, error codes, diagnostics, and logs remain English.

## Overdue Notifications

An Infrastructure SMTP sender implements an Application email abstraction. A hosted service runs periodically and sends notices only when the configured São Paulo local hour has been reached. It queries overdue active/restricted licenses and sends at most one message per company per local calendar day, using `LastOverdueNotificationOn` to enforce idempotency.

The email is sent to the current company owner. It informs the owner that the license is overdue and directs them to the secure authenticated renewal page; it does not attach or expose payment proofs. In-app notices use the same evaluated state and are displayed on the owner dashboard and renewal page.

## Error Handling and Security

All expected license and payment failures use the existing common `OperationResult` contract with feature-owned `Licensing.<Condition>` codes and Web-only Portuguese messages. HTTP endpoints map these errors to `ProblemDetails`; Blazor components render contextual messages and snackbars without raw exception text.

All tenant operations resolve the company from the authenticated user. The company ID is never accepted from an owner request to select the payment target. Administrative operations explicitly require `PlatformAdministrator`; platform roles do not grant access to tenant payment proofs by identifier alone.

The license restriction is applied both to minimal API mutations and direct Blazor use-case calls. A blocked browser request, hidden field, or manual HTTP request cannot create, update, delete, or transition tenant data while the license is read-only.

## Persistence and Migration

EF Core configurations define a unique index for `CompanyLicense.CompanyId`, indexes for payment month/status queries, and a partial/filtered uniqueness approach or transactional repository check for one pending submission per company. A descriptive migration adds the two entities and their foreign keys.

Existing companies receive a license record in the migration with a trial start based on their creation timestamp and a trial end calculated with the established 14-day baseline. EF Core migrations do not read runtime configuration; subsequent company creation uses the configured `FreeTrialDays` value.

## Verification

No automated tests, builds, migrations, application launches, or background hosts are run during implementation unless explicitly requested, in accordance with the repository instructions. Static verification must confirm:

- no tenant mutation bypasses the license write guard;
- owner and employee routes apply the correct evaluated state;
- only platform administrators can open payment proofs or approve submissions;
- PIX and SMTP secrets are not hardcoded;
- proof keys follow the required `pagamentos/{companyId}/{timestamp}.{extension}` pattern;
- expected failures use common operation results;
- no raw exception message is exposed;
- diff whitespace checks are clean.
