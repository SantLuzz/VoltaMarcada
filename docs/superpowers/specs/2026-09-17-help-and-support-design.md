# Help and Support Design

## Goal

Allow an authenticated company member to access practical product guidance, contact VoltaMarcada through WhatsApp, or securely submit a support request with optional image attachments.

## Scope and constraints

- The internal page is available at `/ajuda` and is accessible only to authenticated company members.
- Source-code identifiers, validation codes, logs, and documentation are English. Visible UI copy is Brazilian Portuguese.
- The browser supplies only subject, message, and attachment content. The server resolves the user and company context.
- A support request may contain at most five PNG, JPEG, or WEBP images; each is limited to 5 MiB and all attachments together are limited to 20 MiB.
- Attachment acceptance depends on binary image-signature validation, not filename or declared content type. Accepted attachment content types are derived from the detected signature.
- The request email is addressed to `Smtp:SenderEmail`, uses the authenticated user's email as `Reply-To`, and contains the authenticated user's name/email plus the tenant's trade and legal names in an HTML-encoded body.
- Requests are limited to five per authenticated user in every fixed ten-minute window. The limiter must partition on the authenticated user ID, never client IP.
- `Support:WhatsAppNumber` is optional. Its configuration abstraction strips non-digits, accepts a Brazilian local 10/11-digit number by prepending `55`, accepts a 12/13-digit international number only when it starts with `55`, and exposes `null` for missing or invalid values. The UI renders a WhatsApp link only when a valid number exists.

## Application design

Create a `Support` application feature with:

- `SendSupportRequestCommand(string Subject, string Message, IReadOnlyList<SupportAttachmentInput> Attachments)`.
- `SupportAttachmentInput(string FileName, string DeclaredContentType, byte[] Content)` as the transport-neutral attachment value.
- `SendSupportRequestUseCase`, using `ICurrentUser`, `ICurrentCompany`, an application persistence abstraction or `AppDbContext`-backed existing data-access pattern, and `IEmailSender`.
- `SupportErrors` and explicit operation results for unauthenticated users, missing company membership, missing required text, invalid attachment count/size/total size/signature, unavailable user email, and unavailable support recipient.

The use case trims and bounds text, checks authentication before accessing identifiers, loads the current user and company using both IDs and tenant membership, validates each byte array before creating `EmailAttachment`, and invokes `IEmailSender.SendAsync`. The email subject is prefixed to identify support traffic. The HTML body encodes all user-provided and database-derived strings.

Signature detection recognizes PNG (`89 50 4E 47 0D 0A 1A 0A`), JPEG (`FF D8 FF`), and RIFF/WEBP (`RIFF` at offset zero and `WEBP` at offset eight). It rejects truncated signatures and every other payload, including files whose extension or declared MIME type falsely claims to be an image.

## Web endpoint and configuration design

Add `SupportEndpoints.MapSupportEndpoints` and map `POST /support/requests`. The endpoint accepts multipart form fields `subject`, `message`, and zero or more `attachments`; it translates `IFormFile` streams to bounded byte arrays before calling the use case. It applies `AuthorizationPolicies.CompanyMember` and the `support-requests` rate-limit policy. It returns a successful empty response for delivery acceptance and standardized validation/problem responses for expected failures.

Add `SupportOptions` and `SupportConfiguration` in `VoltaMarcada.Web.Configuration`, with an `ISupportConfiguration` application-facing interface if the page must not reference Web option classes directly. Register options binding and validation at startup, the configuration abstraction, the endpoint mapper, the use case, and the rate-limit policy in `Program.cs`. The policy's partition key uses `ClaimTypes.NameIdentifier`; a missing or invalid claim gets a non-shared rejected partition.

No SMTP credential moves are required: the existing `SmtpEmailSender` and extended `EmailMessage` remain the delivery boundary. Production compose and `.env.production.example` already map `SUPPORT_WHATSAPP_NUMBER`; source tests will assert that contract and that no example value contains secrets.

## UI design

Create `Components/Pages/Help.razor` with `@page "/ajuda"`, company-member authorization, and MudBlazor layout sections for the seven requested topics: registrations, customers/equipment, work orders, schedule, recurring maintenance, quotes, and public requests.

The page displays a WhatsApp button only when `ISupportConfiguration.WhatsAppUrl` is available. Its message says that the channel is for assistance and opens the normalized `wa.me` URL. The form uses `MudForm`, a subject field, message field, MudBlazor-compatible image selection, a list of selected files, client-side guidance, pending/success/error states, and submits multipart data to `/support/requests`. Client-side checks improve feedback but do not replace endpoint/use-case validation.

Add a navigation item named `Ajuda e suporte` to `MainLayout`. It appears in the internal company-context drawer alongside the operational navigation.

## Error handling and security

- The endpoint never binds user name, user email, company name, company ID, or recipient email from client input.
- The use case rechecks authentication and company association, so direct endpoint calls and internal invocations share the same security boundary.
- Rejected attachments are never passed to SMTP.
- User-visible errors are generic and Portuguese; logs avoid including message bodies or attachment data.
- Email sending preserves the current sender behavior. Delivery failures are logged by the sender and are not exposed as SMTP internals to the user.

## Tests and documentation

Add or extend tests covering:

1. `SendSupportRequestUseCase`: valid request builds the expected email recipient, HTML context, reply-to, and detected attachment content type; missing authentication/membership; blank subject/message; attachment count; per-file and aggregate limits; invalid/disguised/truncated image signatures.
2. `SupportEndpoints`: unauthenticated and no-company access rejection, multipart success without trusting client identity fields, validation result mapping, and user-partitioned five-per-ten-minute rate limiting.
3. `SupportConfiguration`: blank/invalid input has no link and formatted Brazilian/international values produce the normalized `https://wa.me/55...` URL.
4. `Help.razor` and `MainLayout`: route, guidance sections, form controls, conditional WhatsApp CTA, and navigation link source contracts.
5. Existing mail and production deployment source tests: `ReplyToEmail` and attachments remain covered; support production configuration maps `SUPPORT_WHATSAPP_NUMBER` while examples remain sanitized.

Create `docs/help-and-support-flows.md` for end-user-facing operational guidance and the support submission flow, and link it from the repository README.
