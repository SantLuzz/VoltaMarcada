# Help and Support Design

## Purpose

Provide authenticated VoltaMarcada users with an in-application help area that explains the principal operational flows and offers direct support through WhatsApp or an email support request.

The support request must be delivered through the existing SMTP infrastructure to the same VoltaMarcada mailbox configured as `Smtp:SenderEmail`. The user can attach screenshots or other error images, and a support operator can reply directly to the user's authenticated email address.

## Scope

- Add an authenticated Help and Support page at `/ajuda`, linked from the main navigation.
- Present concise in-app help for the principal operational flows:
  - getting started and registrations;
  - customers and equipment;
  - work orders and execution;
  - schedule and appointments;
  - recurring maintenance and contextual booking links;
  - quotes;
  - public booking requests.
- Keep a Markdown source document with the same flow reference for maintainers.
- Add a WhatsApp contact action using a configurable support number.
- Add a support request form with subject, message, and image attachments.
- Deliver requests through the existing SMTP sender and make replies go to the authenticated user.

## Configuration

`Support:WhatsAppNumber` is the only new setting. It is a non-secret number formatted for WhatsApp international dialing.

Production maps it from `Support__WhatsAppNumber` in `deploy/compose.production.yml`. The environment example declares the variable with a placeholder only. No real support number, SMTP address, password, or credential is committed.

The support request recipient is deliberately not separately configurable. It is `Smtp:SenderEmail`, which is already supplied from the production environment and identifies the VoltaMarcada mailbox that both sends and receives support messages.

## User Experience

The page uses the established authenticated application shell and MudBlazor components. It has:

1. A page header explaining that users can consult guides or contact VoltaMarcada.
2. Help sections presented as clear expandable cards, using plain Brazilian Portuguese and linking to relevant application pages where helpful.
3. A support contact card with a WhatsApp button. The generated URL contains only the configured number and an optional generic pre-filled message; no tenant or user data is put in the URL.
4. A support form with required subject and message fields, an optional multi-image picker, submission state, and clear success or error feedback.

The form is available only to authenticated company members. The application resolves the company and user identity server-side; no company or user identifiers are accepted from the browser as authority.

## Support Email Flow

1. The browser sends the subject, message, and selected images to an authenticated support endpoint.
2. Server-side validation checks the form values and uploads.
3. The application builds a support email containing the authenticated user's display name, email, company name, submitted subject, and safely encoded message.
4. The email recipient and sender are both `Smtp:SenderEmail`.
5. The message `Reply-To` is the authenticated user's email address, allowing the support operator to reply directly from the VoltaMarcada mailbox.
6. Valid image content is attached to the message and SMTP delivers it through the existing sender.
7. No attachment is persisted after the request lifecycle completes.

## Attachment Safety Rules

The initial release accepts only PNG, JPEG, and WEBP images. A request may contain at most five images, each no larger than 5 MB, with a combined maximum of 20 MB.

Validation is enforced on the server by extension, declared MIME type, and image signature/content inspection before any email is composed. Filenames are treated as display-only data and sanitized for email headers. Invalid images fail the request with a clear validation message. Attachments are held in memory only for the request and disposed after sending; they are neither stored nor made publicly accessible.

## Application Boundaries

The Application project owns a focused `SendSupportRequest` use case, request/response contracts, validation, and a small configuration abstraction for the WhatsApp number. It depends on the existing email abstraction, extended with optional reply-to and attachment data.

Infrastructure owns SMTP-specific message construction, including `MailMessage.ReplyToList` and attachments. Existing password-reset and notification flows remain compatible because attachments and reply-to are optional.

Web owns the `/ajuda` page, multipart request binding, authenticated endpoint, page interaction, and configuration binding. Razor components do not decide the recipient or trust browser-provided tenancy data.

## Error Handling and Abuse Protection

Expected validation failures return structured errors and keep the user-entered form state where possible. SMTP delivery failures are logged without exposing mail-server details and return a generic support-unavailable message.

The endpoint uses the existing authenticated authorization policy and a dedicated request rate limit appropriate for a human support form. It does not log attachment bytes, credentials, or message bodies.

## Testing

Integration tests cover:

- production configuration mapping for `Support__WhatsAppNumber`;
- navigation and authenticated rendering of the help page;
- server-side form validation;
- use-case recipient selection from the SMTP sender setting;
- authenticated user email as reply-to;
- allowed image attachments and rejection of unsupported, oversized, or excessive uploads;
- SMTP message construction with optional reply-to and attachments;
- preservation of existing no-attachment email behavior.

## Non-Goals

- Ticket persistence, support dashboards, ticket statuses, or SLA workflow;
- file storage, public upload URLs, or non-image attachments;
- official WhatsApp Business API integration;
- exposing internal logs or tenant data through support requests.
