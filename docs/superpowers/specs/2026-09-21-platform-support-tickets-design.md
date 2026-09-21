# Platform Support Tickets Design

## Goal

Replace the current email-only support request flow with durable platform support tickets. Company users create tickets from the Help page. Platform administrators review tickets, attachments, and message history, reply through the configured SMTP provider, and explicitly close tickets.

The first release does not provide a company-side ticket history page, real-time chat, incoming email processing, or email replies from users.

## User Flow

1. An authenticated company member opens the Help page and submits a subject, message, and up to five supported images.
2. The system creates a support ticket with an immutable, platform-wide sequential display number and `Open` status.
3. The Help page clears its form and displays `Chamado nº {number} aberto.`.
4. A platform administrator opens `/plataforma/chamados`, filters open or closed tickets, and opens a ticket.
5. The administrator views the company, requester, opening date and time, messages, and image attachments.
6. The administrator writes a response. The system sends a branded HTML email to the requester's email address, records the outgoing message, and keeps the ticket open.
7. The administrator explicitly closes the ticket when the matter is resolved.

## Domain Model

### SupportTicket

`SupportTicket` is a platform-owned aggregate and is not tenant-filtered for platform administrator operations. It retains the originating company and requester identifiers for auditability.

Fields:

- `Id` (`Guid`)
- `Number` (`long`), unique and sequential for the platform
- `CompanyId` (`Guid`)
- `RequesterUserId` (`Guid`)
- requester name and email snapshots
- company trade name snapshot
- `Subject`
- `Status` (`Open`, `Closed`)
- `OpenedAtUtc`
- `ClosedAtUtc` and `ClosedByUserId`, nullable
- message collection

The first message is created with the ticket and represents the company request. A ticket cannot be closed twice and a closed ticket cannot receive a new administrator response in this initial release.

The ticket number is allocated by a database-backed sequence or equivalent concurrency-safe persistence mechanism. The unique index is a final guard against duplicate display numbers.

### SupportTicketMessage

Fields:

- `Id` (`Guid`)
- `SupportTicketId` (`Guid`)
- `Direction` (`Customer`, `Platform`)
- message body
- `CreatedAtUtc`
- `SentByUserId`, nullable for the original customer message
- `EmailSentAtUtc`, nullable; populated only after successful SMTP delivery
- attachment collection

### SupportTicketAttachment

Each attachment belongs to one message and stores its Cloudinary file key, original file name, detected content type, and length. The same image constraints already applied by the Help page remain in force: PNG, JPEG, or WEBP; maximum five files; maximum 5 MB per file and 20 MB total.

## Application Behavior

`CreateSupportTicket` replaces `SendSupportRequest` as the Help page command. It validates authenticated user and company context, validates the request and images, uploads valid attachments under a platform ticket path, and persists the ticket and initial message in one application flow. It returns the display number.

`GetPlatformSupportTickets` returns platform-wide list projections, filterable by status. `GetPlatformSupportTicketDetails` loads one ticket, all messages, and attachment metadata. Both require platform administrator authorization at the Web boundary and do not accept a browser-supplied company scope.

`ReplyToSupportTicket` validates that the ticket is open and the response is non-empty. It creates an outgoing message, renders the email, sends it through `IEmailSender`, then marks the message as sent and persists it. SMTP failure returns a user-safe failure and leaves no sent message in the history.

`CloseSupportTicket` explicitly transitions an open ticket to closed and records the platform administrator and time.

### Email Delivery Contract

`IEmailSender` returns an explicit delivery result rather than swallowing SMTP failures. A disabled SMTP configuration, invalid configuration, or SMTP exception returns a safe failure result and is logged by Infrastructure. Existing callers that do not need to surface delivery status may log the failure; `ReplyToSupportTicket` must show a Portuguese retryable error and must not persist an outgoing message as sent.
## Email Template

The outgoing subject is:

```text
Res.: chamado nº {number}
```

The email is table-based responsive HTML for common email clients. It has a VoltaMarcada brand-color header, the public logo, a white content card, the ticket number, the reply text, and a muted footer.

The logo URL is built from `ApplicationUrls:ApplicationBaseUrl` and `/logos/logo.png`. The configured base URL must be an absolute public URL in non-development environments.

The rendered text content is:

```text
Olá, {name}

Seu chamado nº {number} foi respondido pela equipe de suporte.

{reply}

Att. VoltaMarcada

Não responda este e-mail.
```

The requester email is the recipient. The configured SMTP sender remains the sender. The email is intentionally not configured with a reply-to address because incoming email replies are out of scope.

## Web UI

The Help form keeps its guide and WhatsApp functionality. On success it displays the ticket number rather than a generic success message. It does not send email.

`PlatformSupportTickets.razor` uses `PlatformLayout` and `PlatformAdministrator` authorization. The list displays number, status, opened date/time, company, requester, subject, and actions. The detail view shows chronology, safe image links, a response editor, a send action, and a close action. Closed tickets are read-only.

The platform navigation gains a `Chamados` entry visible only to platform administrators.

## Persistence and Security

EF Core configurations define the ticket number unique index, ticket/message foreign keys, attachment foreign keys, and the appropriate field lengths. A migration adds the three tables and their indexes. Attachment uploads are cleaned up if ticket creation cannot be persisted.

Only the creation command resolves company and requester from authenticated server-side context. All ticket administration routes and attachment-content routes require `PlatformAdministrator`. Attachment keys are never accepted from the browser when reading content. Attachments are served only after the administrator authorization check.

## Error Handling

The Help page shows a Portuguese validation or persistence error without exposing storage or SMTP details. The administrator sees a Portuguese retryable error when SMTP delivery fails. A ticket is never silently reported as replied if the outgoing email was not delivered.

## Tests

Automated tests cover ticket creation with its number, requester and attachment validation, platform-only access, list/detail projections, close transition, successful reply email composition, SMTP failure behavior, and protected attachment content. The HTML email test asserts the recipient, subject, escaped reply content, and logo URL derived from the configured base URL.
