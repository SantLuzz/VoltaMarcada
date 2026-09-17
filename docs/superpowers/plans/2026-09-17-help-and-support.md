# Help and Support Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add authenticated in-app help, WhatsApp contact, and SMTP support requests with secure image attachments.

**Architecture:** Web binds support configuration and converts an authenticated multipart request into an Application command. Application validates and composes an extended mail message; Infrastructure maps reply-to and attachments to SMTP. Sender and recipient are both `Smtp:SenderEmail`.

**Tech Stack:** .NET 10, ASP.NET Core Minimal APIs, Blazor Server, MudBlazor, FluentValidation, System.Net.Mail, xUnit.

**Spec:** `docs/superpowers/specs/2026-09-17-help-and-support-design.md`

## Global Constraints

- Source, identifiers, tests, logs, and technical documentation are English; UI text is Brazilian Portuguese.
- No additional dependencies.
- Resolve authenticated user and company on the server; no tenant identity from browser fields.
- Allow PNG/JPEG/WEBP only: max 5 files, 5 MB each, 20 MB total; validate declared MIME, extension, and signature; do not persist attachments.
- Add only `Support:WhatsAppNumber`, mapped as `Support__WhatsAppNumber`; never commit real secrets or support data.

---

### Task 1: Secure configuration and deployment example

**Files:** Modify `deploy/.env.production.example`, `deploy/compose.production.yml`, `src/VoltaMarcada.Web/appsettings.json`, and `tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs`.

**Produces:** `Support__WhatsAppNumber: ${SUPPORT_WHATSAPP_NUMBER}` and safe placeholders only.

- [ ] **Step 1: Write a failing source test**

```csharp
Assert.Contains("Support__WhatsAppNumber: ${SUPPORT_WHATSAPP_NUMBER}", compose);
Assert.Equal(string.Empty, settings.RootElement.GetProperty("Support").GetProperty("WhatsAppNumber").GetString());
Assert.Contains("SUPPORT_WHATSAPP_NUMBER=", environmentExample);
Assert.DoesNotContain("Password=", environmentExample, StringComparison.OrdinalIgnoreCase);
```

- [ ] **Step 2: Run red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~ProductionDeploymentSourceTests`

Expected: FAIL because support mapping is absent and the example contains values.

- [ ] **Step 3: Implement**

```json
"Support": { "WhatsAppNumber": "" }
```

```yaml
Support__WhatsAppNumber: ${SUPPORT_WHATSAPP_NUMBER}
```

Replace all populated example environment values with blank values while retaining variable names and comments. Add `SUPPORT_WHATSAPP_NUMBER=`.

- [ ] **Step 4: Run green and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~ProductionDeploymentSourceTests`

Commit: `git add deploy/.env.production.example deploy/compose.production.yml src/VoltaMarcada.Web/appsettings.json tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs; git commit -m "chore: configure support safely"`

### Task 2: Extend the reusable mail contract

**Files:** Modify `src/VoltaMarcada.Application/Abstractions/Communication/IEmailSender.cs`, `src/VoltaMarcada.Infrastructure/Communication/SmtpEmailSender.cs`, and `tests/VoltaMarcada.IntegrationTests/Communication/EmailMessageTests.cs`; create `tests/VoltaMarcada.IntegrationTests/Communication/SmtpEmailSenderTests.cs`.

**Produces:** `EmailAttachment`, optional `EmailMessage.ReplyToEmail`, and optional `EmailMessage.Attachments`; existing three-argument usage remains valid.

- [ ] **Step 1: Write failing tests**

```csharp
var email = new EmailMessage("support@test", "Need help", "<p>Text</p>", "user@test", [new EmailAttachment("screen.png", "image/png", [0x89])]);
Assert.Equal("user@test", email.ReplyToEmail);
Assert.Single(email.Attachments);
```

- [ ] **Step 2: Run red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~EmailMessageTests|FullyQualifiedName~SmtpEmailSenderTests"`

Expected: FAIL because optional message metadata is absent.

- [ ] **Step 3: Implement**

```csharp
public sealed record EmailAttachment(string FileName, string ContentType, byte[] Content);
public sealed record EmailMessage(string RecipientEmail, string Subject, string HtmlBody, string? ReplyToEmail = null, IReadOnlyList<EmailAttachment>? Attachments = null);
```

Normalize null attachments to an empty list, strip CR/LF from every header and filename, add `ReplyToList` only when set, and create attachments from in-memory streams in the `MailMessage` disposal scope.

- [ ] **Step 4: Run green and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~EmailMessageTests|FullyQualifiedName~SmtpEmailSenderTests|FullyQualifiedName~AccountPasswordResetEmailTests|FullyQualifiedName~PublicBookingNotificationSenderTests"`

Commit: `git add src/VoltaMarcada.Application/Abstractions/Communication/IEmailSender.cs src/VoltaMarcada.Infrastructure/Communication/SmtpEmailSender.cs tests/VoltaMarcada.IntegrationTests/Communication; git commit -m "feat: support email attachments"`

### Task 3: Implement validated support email use case

**Files:** Create `src/VoltaMarcada.Application/Support/SupportContracts.cs` and `SendSupportRequestUseCase.cs`; modify `src/VoltaMarcada.Application/DependencyInjection.cs`; create `tests/VoltaMarcada.IntegrationTests/Support/SendSupportRequestUseCaseTests.cs` and `SupportAttachmentValidationTests.cs`.

**Produces:** `SupportRequestCommand`, `SupportImageAttachment`, `ISupportEmailRecipient`, and `SendSupportRequestUseCase.ExecuteAsync`.

- [ ] **Step 1: Write failing use-case tests**

```csharp
var result = await useCase.ExecuteAsync(new SupportRequestCommand("Agenda", "<script>x</script>", "Ana", "ana@company.test", "Ana Services", []));
Assert.True(result.IsSuccess);
Assert.Equal("support@voltamarcada.test", sender.Message!.RecipientEmail);
Assert.Equal("ana@company.test", sender.Message.ReplyToEmail);
Assert.Contains("&lt;script&gt;x&lt;/script&gt;", sender.Message.HtmlBody, StringComparison.Ordinal);
```

- [ ] **Step 2: Run red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~SendSupportRequestUseCaseTests|FullyQualifiedName~SupportAttachmentValidationTests"`

Expected: FAIL because the Support namespace is absent.

- [ ] **Step 3: Implement**

Validate subject 1–160, message 1–4,000, requester email, zero-to-five files, 5 MB/file, 20 MB total. Return explicit `Support.InvalidInput`, `Support.InvalidAttachment`, `Support.TooManyAttachments`, or `Support.AttachmentsTooLarge` errors. Verify PNG `89 50 4E 47 0D 0A 1A 0A`, JPEG `FF D8 FF`, or RIFF/WEBP bytes; encode all user values in HTML; send to `ISupportEmailRecipient` with requester email as reply-to. Register the use case.

- [ ] **Step 4: Run green and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~SendSupportRequestUseCaseTests|FullyQualifiedName~SupportAttachmentValidationTests"`

Commit: `git add src/VoltaMarcada.Application/Support src/VoltaMarcada.Application/DependencyInjection.cs tests/VoltaMarcada.IntegrationTests/Support; git commit -m "feat: send support requests"`

### Task 4: Add protected support endpoint, configuration, and rate limit

**Files:** Create `src/VoltaMarcada.Web/Configuration/SupportOptions.cs`, `SupportConfiguration.cs`, and `Endpoints/SupportEndpoints.cs`; modify `src/VoltaMarcada.Web/Program.cs`; create `tests/VoltaMarcada.IntegrationTests/Support/SupportEndpointsTests.cs`.

**Produces:** `POST /support/requests`, `ISupportConfiguration`, and `ISupportEmailRecipient` adapters.

- [ ] **Step 1: Write failing endpoint tests**

```csharp
var anonymous = await fixture.Client.PostAsync("/support/requests", new MultipartFormDataContent());
Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);
var authenticated = await fixture.AuthenticatedClient.PostAsync("/support/requests", CreateSupportForm("Printer error", "Unable to save"));
Assert.Equal(HttpStatusCode.OK, authenticated.StatusCode);
Assert.Equal("owner@company.test", fixture.EmailSender.Message!.ReplyToEmail);
```

- [ ] **Step 2: Run red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter FullyQualifiedName~SupportEndpointsTests`

Expected: FAIL with an unmapped route.

- [ ] **Step 3: Implement**

Bind options with `ValidateOnStart`; allow an empty WhatsApp number or 10–15 digits after normalization. Create the `wa.me` URL only from normalized config and return `SmtpOptions.SenderEmail` from the email-recipient adapter. Require `CompanyMember`, resolve `ICurrentUser`, `ICurrentCompany`, user manager, and company repository server-side, and bind only subject/message/files. Enforce a five-per-ten-minute fixed-window policy partitioned by user ID. Stream each upload at 5 MB + 1 and hand byte data to the use case.

- [ ] **Step 4: Run green and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~SupportEndpointsTests|FullyQualifiedName~ProductionDeploymentSourceTests"`

Commit: `git add src/VoltaMarcada.Web/Configuration src/VoltaMarcada.Web/Endpoints/SupportEndpoints.cs src/VoltaMarcada.Web/Program.cs tests/VoltaMarcada.IntegrationTests/Support/SupportEndpointsTests.cs; git commit -m "feat: add support endpoint"`

### Task 5: Create Help UI, guide document, and complete verification

**Files:** Create `src/VoltaMarcada.Web/Components/Pages/Help.razor`, `docs/help-and-support-flows.md`, `tests/VoltaMarcada.IntegrationTests/Components/HelpRenderingTests.cs`, and `tests/VoltaMarcada.IntegrationTests/Support/HelpDocumentationTests.cs`; modify `MainLayout.razor`, `MainLayoutTests.cs`, and `README.md`.

**Produces:** member-only `/ajuda` with navigation and canonical flow guide.

- [ ] **Step 1: Write failing rendering and documentation tests**

```csharp
Assert.Contains("Clientes e equipamentos", html);
Assert.Contains("Ordens de serviço", html);
Assert.Contains("Agenda e agendamentos", html);
Assert.Contains("Manutenção recorrente", html);
Assert.Contains("Orçamentos", html);
Assert.Contains("Solicitações públicas", html);
Assert.Contains("https://wa.me/5511999999999", html);
Assert.Contains("/ajuda", mainLayoutHtml);
Assert.Contains("docs/help-and-support-flows.md", readme);
```

- [ ] **Step 2: Run red**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~HelpRenderingTests|FullyQualifiedName~HelpDocumentationTests|FullyQualifiedName~MainLayoutTests"`

Expected: FAIL because Help and its guide do not exist.

- [ ] **Step 3: Implement**

Use an authorized `Help.razor`, `PageHeader`, and seven MudBlazor expansion panels: first steps, customers/equipment, work orders, schedule, recurring maintenance, quotes, and public requests. Add the WhatsApp MudButton only with a configured number, `Target="_blank"`, and `rel="noopener noreferrer"`. Use outlined `MudTextField` controls, `InputFile` for the only unavailable MudBlazor equivalent, client prechecks, multipart POST, pending state, Portuguese snackbars, and reset only after success. Add a HelpOutline member nav link. Write an English maintainer guide that states attachments are sent only by e-mail and never stored; link it from README.

- [ ] **Step 4: Run final verification and commit**

Run: `dotnet build VoltaMarcada.sln --no-restore`

Run: `dotnet test VoltaMarcada.sln --no-build`

Expected: both commands exit 0 with no errors or failed tests.

Commit: `git add src/VoltaMarcada.Web/Components/Pages/Help.razor src/VoltaMarcada.Web/Components/Layout/MainLayout.razor docs/help-and-support-flows.md README.md tests/VoltaMarcada.IntegrationTests/Components tests/VoltaMarcada.IntegrationTests/Support; git commit -m "feat: add help and support experience"`
