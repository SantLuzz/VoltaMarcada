# Platform Support Tickets Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace email-only Help requests with persistent, platform-administered support tickets that retain images, support branded SMTP replies, and can be explicitly closed.

**Architecture:** Add a platform-owned `SupportTicket` aggregate with message and attachment children. The Help endpoint creates tickets using authenticated company context and storage; platform-administrator use cases list, read, reply, and close tickets. Reply email rendering is isolated behind an application template and delivery returns an explicit result so failed SMTP attempts cannot be reported as sent.

**Tech Stack:** .NET 10, C#, ASP.NET Core, Blazor Web App, EF Core/PostgreSQL, ASP.NET Core Identity, Cloudinary storage, SMTP, xUnit, MudBlazor.

**Spec:** `docs/superpowers/specs/2026-09-21-platform-support-tickets-design.md`

## Global Constraints

- All source identifiers, migrations, test names, comments, and technical documentation are English; Brazilian Portuguese is only for user-facing copy.
- Ticket administration requires `AuthorizationPolicies.PlatformAdministrator`; company and requester identity are server-resolved.
- Ticket display numbers are global, sequential, database-backed, immutable, and protected by a unique index.
- Attachments allow PNG, JPEG, and WEBP only; maximum five, 5 MB each, and 20 MB combined.
- Use `TimeProvider` in application behavior and persist timestamps as UTC.
- Reuse `IFileStorage`, `IEmailSender`, MudBlazor, and the platform layout. Do not add dependencies.
- Use `{ApplicationUrls:ApplicationBaseUrl}/logos/logo.png` as the public email logo URL.

## Review Focus

- Concurrent creation must not duplicate a number; Task 2 verifies a database-backed allocator.
- Disabled or failed SMTP must show a retryable response error rather than a false success; Task 4 verifies it.
- Declared image types must not bypass byte-content validation; Task 2 verifies it.
- Company members must not list tickets or download images; Task 5 verifies platform-only routes.
- Closed tickets must reject responses and retain history; Task 4 verifies it.

---

## File Structure

| Path | Responsibility |
| --- | --- |
| `src/VoltaMarcada.Domain/Entities/SupportTicket.cs` | Ticket state, numbering, messages, and closing. |
| `src/VoltaMarcada.Domain/Entities/SupportTicketMessage.cs` | Inbound/outbound messages. |
| `src/VoltaMarcada.Domain/Entities/SupportTicketAttachment.cs` | Attachment metadata. |
| `src/VoltaMarcada.Domain/Enums/SupportTicket*.cs` | Ticket status and message direction enums. |
| `src/VoltaMarcada.Application/Support/*SupportTicket*.cs` | Commands, projections, use cases, and email template. |
| `src/VoltaMarcada.Application/Abstractions/Persistence/ISupportTicketRepository.cs` | Ticket persistence contract. |
| `src/VoltaMarcada.Infrastructure/Persistence/Configurations/*SupportTicket*.cs` | EF mappings. |
| `src/VoltaMarcada.Infrastructure/Persistence/Repositories/SupportTicketRepository.cs` | EF repository and number allocation. |
| `src/VoltaMarcada.Web/Endpoints/PlatformSupportTicketEndpoints.cs` | Platform endpoints and attachment content. |
| `src/VoltaMarcada.Web/Components/Pages/PlatformSupportTickets.razor` | Administrator list, detail, reply, and close UI. |
| `tests/VoltaMarcada.IntegrationTests/Support/SupportTicket*.cs` | Ticket behavior and route tests. |

## Tasks

### Task 1: Add domain entities, EF mappings, and migration

**Files:**
- Create: `src/VoltaMarcada.Domain/Enums/SupportTicketStatus.cs`
- Create: `src/VoltaMarcada.Domain/Enums/SupportTicketMessageDirection.cs`
- Create: `src/VoltaMarcada.Domain/Entities/SupportTicket.cs`
- Create: `src/VoltaMarcada.Domain/Entities/SupportTicketMessage.cs`
- Create: `src/VoltaMarcada.Domain/Entities/SupportTicketAttachment.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/SupportTicketConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/SupportTicketMessageConfiguration.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Configurations/SupportTicketAttachmentConfiguration.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Persistence/AppDbContext.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Support/SupportTicketDomainTests.cs`

**Consumes:** Existing domain `Company`, `DomainException`, EF configuration conventions.

**Produces:**

```csharp
public enum SupportTicketStatus { Open, Closed }
public enum SupportTicketMessageDirection { Customer, Platform }
public sealed class SupportTicket
{
    public static SupportTicket Create(long number, Guid companyId, Guid requesterUserId,
        string requesterName, string requesterEmail, string companyTradeName, string subject,
        string message, DateTimeOffset openedAtUtc);
    public SupportTicketMessage AddPlatformMessage(string body, Guid sentByUserId, DateTimeOffset createdAtUtc);
    public void Close(Guid closedByUserId, DateTimeOffset closedAtUtc);
}
```

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public void Create_AssignsOpenStatusAndInitialCustomerMessage()
{
    var ticket = SupportTicket.Create(42, Guid.NewGuid(), Guid.NewGuid(), "Maria", "maria@example.com", "Empresa", "Ajuda", "Mensagem", Now);
    Assert.Equal(SupportTicketStatus.Open, ticket.Status);
    Assert.Single(ticket.Messages);
}

[Fact]
public void AddPlatformMessage_WhenTicketIsClosed_ThrowsDomainException()
{
    var ticket = CreateTicket();
    ticket.Close(Guid.NewGuid(), Now);
    Assert.Throws<DomainException>(() => ticket.AddPlatformMessage("Resposta", Guid.NewGuid(), Now));
}
```

- [ ] **Step 2: Run the tests to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~SupportTicketDomainTests"`

Expected: failure because support-ticket types do not exist.

- [ ] **Step 3: Implement minimal domain and mapping behavior**

Use private setters, UTC normalization, owned child collections, max lengths of 200 for subject, 5000 for message, 1000 for storage key, and 255 for file name/content type. Add a unique `Number` index and cascade ticket-to-message-to-attachment relationships.

- [ ] **Step 4: Verify GREEN and generate migration**

Run the Task 1 test filter, then:

`dotnet ef migrations add AddSupportTickets --project src/VoltaMarcada.Infrastructure --startup-project src/VoltaMarcada.Web`

Inspect the migration for three English-named tables, foreign keys, indexes, and a PostgreSQL sequence used by `SupportTicket.Number`.

- [ ] **Step 5: Commit**

`git add src/VoltaMarcada.Domain src/VoltaMarcada.Infrastructure/Persistence tests/VoltaMarcada.IntegrationTests/Support`

`git commit -m "feat: add support ticket persistence"`

### Task 2: Create ticket command, storage flow, and repository

**Files:**
- Create: `src/VoltaMarcada.Application/Abstractions/Persistence/ISupportTicketRepository.cs`
- Create: `src/VoltaMarcada.Application/Support/SupportTicketCommands.cs`
- Create: `src/VoltaMarcada.Application/Support/CreateSupportTicketUseCase.cs`
- Create: `src/VoltaMarcada.Infrastructure/Persistence/Repositories/SupportTicketRepository.cs`
- Modify: `src/VoltaMarcada.Application/DependencyInjection.cs`
- Modify: `src/VoltaMarcada.Infrastructure/DependencyInjection.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Support/CreateSupportTicketUseCaseTests.cs`

**Consumes:** Task 1, `ICurrentUser`, `ICurrentCompany`, `ISupportRequestContextReader`, `IFileStorage`, `SupportAttachmentDetector`, `TimeProvider`.

**Produces:**

```csharp
public sealed record CreateSupportTicketCommand(string Subject, string Message, IReadOnlyList<SupportAttachmentInput> Attachments);
public sealed record SupportTicketCreationResult(Guid TicketId, long TicketNumber);
public interface ISupportTicketRepository
{
    Task<long> GetNextNumberAsync(CancellationToken cancellationToken = default);
    Task AddAsync(SupportTicket ticket, CancellationToken cancellationToken = default);
    Task SaveChangesAsync(CancellationToken cancellationToken = default);
}
```

- [ ] **Step 1: Write failing tests**

```csharp
[Fact]
public async Task ExecuteAsync_WithValidRequest_CreatesTicketAndReturnsNumber()
{
    var result = await fixture.UseCase.ExecuteAsync(new("Acesso", "Não consigo entrar", []));
    Assert.True(result.IsSuccess);
    Assert.Equal(1, result.Value!.TicketNumber);
}

[Fact]
public async Task ExecuteAsync_WithDisguisedImage_ReturnsInvalidAttachmentWithoutUpload()
{
    var result = await fixture.UseCase.ExecuteAsync(fixture.WithAttachment("photo.png", "image/png", "not an image"u8.ToArray()));
    Assert.Equal(SupportErrors.InvalidAttachment, result.Error);
    Assert.Empty(fixture.Storage.Uploads);
}
```

- [ ] **Step 2: Run the filter to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~CreateSupportTicketUseCaseTests"`

Expected: failure because the ticket command and use case do not exist.

- [ ] **Step 3: Implement creation and cleanup**

Resolve requester/company on the server, validate subject/message/count/size/detected byte type, allocate a database number, upload files under `platform/support-tickets/{ticketId:N}/messages/{messageId:N}`, and persist the aggregate. Delete every already-uploaded file if a later upload or persistence step fails.

- [ ] **Step 4: Add sequential-number coverage and verify GREEN**

Create tickets in separate repository contexts and assert distinct consecutive numbers. Run the Task 2 filter and expect PASS.

- [ ] **Step 5: Commit**

`git add src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure tests/VoltaMarcada.IntegrationTests/Support`

`git commit -m "feat: create support tickets"`

### Task 3: Replace Help email submission with ticket creation

**Files:**
- Modify: `src/VoltaMarcada.Web/Endpoints/SupportEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Components/Pages/Help.razor`
- Remove or retire: `src/VoltaMarcada.Application/Support/SendSupportRequestUseCase.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Support/SupportTicketEndpointTests.cs`

**Consumes:** `CreateSupportTicketUseCase.ExecuteAsync` from Task 2.

**Produces:** `POST /support/requests` returns 201 with `CreatedSupportTicketResponse(Guid TicketId, long TicketNumber)`; Help displays `Chamado nº {number} aberto.`.

- [ ] **Step 1: Write failing endpoint tests**

```csharp
[Fact]
public async Task Post_WithValidSupportRequest_ReturnsCreatedTicketNumber()
{
    var response = await fixture.PostAsync("Assunto", "Mensagem", []);
    Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    Assert.Equal(1, (await response.Content.ReadFromJsonAsync<CreatedSupportTicketResponse>())!.TicketNumber);
}
```

- [ ] **Step 2: Run the filter to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~SupportTicketEndpointTests"`

Expected: failure because the endpoint returns 200 from `SendSupportRequestUseCase`.

- [ ] **Step 3: Implement endpoint and Help response handling**

Preserve multipart limits and Portuguese validation. Replace SMTP execution with ticket creation, return `Results.Created`, parse the JSON ticket number in `Help.razor`, clear values/attachments/reset validation, and show the numbered confirmation. Remove unused support-delivery configuration only after no caller remains.

- [ ] **Step 4: Verify GREEN and commit**

Run the Task 3 test filter and expect PASS.

`git add src/VoltaMarcada.Web src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure tests/VoltaMarcada.IntegrationTests/Support`

`git commit -m "feat: create support tickets from help"`

### Task 4: Make SMTP delivery observable and implement reply/close use cases

**Files:**
- Modify: `src/VoltaMarcada.Application/Abstractions/Communication/IEmailSender.cs`
- Modify: `src/VoltaMarcada.Infrastructure/Communication/SmtpEmailSender.cs`
- Modify: every caller and email test double under `src/` and `tests/`
- Create: `src/VoltaMarcada.Application/Support/SupportReplyEmailTemplate.cs`
- Create: `src/VoltaMarcada.Application/Support/ReplyToSupportTicketUseCase.cs`
- Create: `src/VoltaMarcada.Application/Support/CloseSupportTicketUseCase.cs`
- Modify: `src/VoltaMarcada.Application/Abstractions/Persistence/ISupportTicketRepository.cs`
- Create: `tests/VoltaMarcada.IntegrationTests/Support/ReplyToSupportTicketUseCaseTests.cs`

**Consumes:** Tasks 1-2, `IEmailSender`, `TimeProvider`, and public application base URL configuration.

**Produces:**

```csharp
public sealed record EmailDeliveryResult(bool IsDelivered, string? ErrorCode);
public interface IEmailSender
{
    Task<EmailDeliveryResult> SendAsync(EmailMessage email, CancellationToken cancellationToken = default);
}
public sealed record ReplyToSupportTicketCommand(string Message);
```

- [ ] **Step 1: Write failing reply, close, and template tests**

```csharp
[Fact]
public async Task ExecuteAsync_WhenDelivered_SendsBrandedEmailAndStoresMessage()
{
    var result = await fixture.ReplyUseCase.ExecuteAsync(fixture.OpenTicketId, new("Tudo foi ajustado."));
    Assert.True(result.IsSuccess);
    Assert.Equal("Res.: chamado nº 000001", fixture.Email.LastMessage!.Subject);
    Assert.Contains("https://app.example.com/logos/logo.png", fixture.Email.LastMessage.HtmlBody);
}

[Fact]
public async Task ExecuteAsync_WhenDeliveryFails_ReturnsFailureAndDoesNotStoreMessage()
{
    fixture.Email.Result = new(false, "smtp-unavailable");
    var result = await fixture.ReplyUseCase.ExecuteAsync(fixture.OpenTicketId, new("Resposta"));
    Assert.False(result.IsSuccess);
    Assert.Single((await fixture.GetTicketAsync()).Messages);
}
```

Add tests for closing and replying to a closed ticket.

- [ ] **Step 2: Run the filter to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~ReplyToSupportTicketUseCaseTests"`

Expected: failure because delivery result and support reply use cases do not exist.

- [ ] **Step 3: Implement explicit delivery and replies**

Return a failure result when SMTP is disabled, invalid, or raises an exception; log its technical cause. Update every existing email caller to deliberately handle the result. Render a table-based inline-style HTML message with encoded values, subject `Res.: chamado nº {number:000000}`, the exact approved Portuguese body, and logo URL `baseUrl.TrimEnd('/') + "/logos/logo.png"`. Persist the outbound message only after successful delivery. Close records UTC time and platform administrator identity.

- [ ] **Step 4: Verify GREEN and commit**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~ReplyToSupportTicketUseCaseTests|FullyQualifiedName~EmailMessageTests"`

Expected: PASS.

`git add src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure tests/VoltaMarcada.IntegrationTests`

`git commit -m "feat: reply to and close support tickets"`

### Task 5: Add protected platform endpoints and administration UI

**Files:**
- Create: `src/VoltaMarcada.Application/Support/GetPlatformSupportTicketsUseCase.cs`
- Create: `src/VoltaMarcada.Application/Support/GetPlatformSupportTicketDetailsUseCase.cs`
- Create: `src/VoltaMarcada.Application/Support/GetPlatformSupportTicketAttachmentContentUseCase.cs`
- Modify: `ISupportTicketRepository.cs` and `SupportTicketRepository.cs`
- Create: `src/VoltaMarcada.Web/Endpoints/PlatformSupportTicketEndpoints.cs`
- Modify: `src/VoltaMarcada.Web/Program.cs`
- Create: `src/VoltaMarcada.Web/Components/Pages/PlatformSupportTickets.razor`
- Modify: `src/VoltaMarcada.Web/Components/Layout/PlatformLayout.razor`
- Create: `tests/VoltaMarcada.IntegrationTests/Support/PlatformSupportTicketEndpointsTests.cs`

**Consumes:** Tasks 1-4, `AuthorizationPolicies.PlatformAdministrator`, `IFileStorage`, and the existing platform layout.

**Produces:**

```text
GET  /platform/support-tickets?status=Open
GET  /platform/support-tickets/{ticketId}
POST /platform/support-tickets/{ticketId}/reply
POST /platform/support-tickets/{ticketId}/close
GET  /platform/support-tickets/{ticketId}/attachments/{attachmentId}/content
```

- [ ] **Step 1: Write failing authorization and content tests**

```csharp
[Theory]
[InlineData(CompanyUserRole.Owner, HttpStatusCode.Forbidden)]
[InlineData(CompanyUserRole.Employee, HttpStatusCode.Forbidden)]
public async Task GetTickets_RequiresPlatformAdministrator(CompanyUserRole role, HttpStatusCode expected)
{
    var response = await fixture.GetTicketsAsync(role);
    Assert.Equal(expected, response.StatusCode);
}

[Fact]
public async Task GetAttachmentContent_AsPlatformAdministrator_ReturnsStoredImage()
{
    var response = await fixture.GetAttachmentAsync(fixture.TicketId, fixture.AttachmentId);
    Assert.Equal("image/png", response.Content.Headers.ContentType!.MediaType);
}
```

- [ ] **Step 2: Run the filter to verify RED**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --filter "FullyQualifiedName~PlatformSupportTicketEndpointsTests"`

Expected: failure because platform ticket routes do not exist.

- [ ] **Step 3: Implement routes and page**

Apply platform-administrator authorization to every route. Confirm attachment membership before storage download. Add an Open/Closed filter and list columns for number, opening date/time, company, requester, subject, status, and action. The detail panel renders chronology, image links, a Portuguese response editor, send action, and explicit close action; closed tickets are read-only. Add `Chamados` to `PlatformLayout`.

- [ ] **Step 4: Verify GREEN and commit**

Run the Task 5 filter, then `dotnet build src/VoltaMarcada.Web/VoltaMarcada.Web.csproj --no-restore --configuration Release`.

`git add src/VoltaMarcada.Application src/VoltaMarcada.Infrastructure src/VoltaMarcada.Web tests/VoltaMarcada.IntegrationTests`

`git commit -m "feat: manage platform support tickets"`

### Task 6: Complete documentation and full verification

**Files:**
- Modify: `README.md`
- Modify: `docs/help-and-support-flows.md`
- Modify: SMTP configuration documentation affected by `EmailDeliveryResult`

**Consumes:** Tasks 1-5.

- [ ] **Step 1: Add the public-logo configuration test**

```csharp
[Fact]
public void Render_WhenApplicationBaseUrlIsPublic_UsesPublicLogoUrl()
{
    var email = SupportReplyEmailTemplate.Render("https://app.example.com", "Maria", 7, "Resposta");
    Assert.Contains("https://app.example.com/logos/logo.png", email.HtmlBody);
}
```

- [ ] **Step 2: Document operations**

Document the Help-to-ticket flow, `/plataforma/chamados`, SMTP requirements, public `ApplicationUrls:ApplicationBaseUrl`, and the intentionally unsupported inbound email replies. Do not commit secrets.

- [ ] **Step 3: Run final verification**

Run: `dotnet test tests/VoltaMarcada.IntegrationTests/VoltaMarcada.IntegrationTests.csproj --no-restore --configuration Release`

Run: `dotnet build src/VoltaMarcada.Web/VoltaMarcada.Web.csproj --no-restore --configuration Release`

Expected: all tests pass and build succeeds; report environment-dependent pre-existing failures separately.

- [ ] **Step 4: Commit**

`git add README.md docs tests/VoltaMarcada.IntegrationTests`

`git commit -m "docs: document support ticket operations"`