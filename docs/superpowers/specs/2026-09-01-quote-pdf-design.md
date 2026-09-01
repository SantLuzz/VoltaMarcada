# Quote PDF Design

## Purpose

Add a server-generated PDF representation of a quote. Company members can download a PDF for every quote in their own tenant, including drafts used for review. Customers can download a PDF from a valid public quote page without gaining access to private data or internal identifiers.

The PDF complements the existing quote lifecycle. It does not change quote statuses, public-token lifetime, work-order generation, quote persistence, or the public approval/rejection flow.

## Scope

The increment includes:

- a framework-neutral application contract for generating a quote PDF;
- authenticated and public application use cases that authorize and prepare the document data;
- a QuestPDF implementation in Infrastructure;
- authenticated and public HTTP download endpoints that return `application/pdf`;
- download actions on the internal quote registration page and public quote page;
- deterministic tests for authorization, public availability, document composition, and HTTP response metadata.

The increment does not persist generated files, create a database migration, send PDFs as attachments, or change the WhatsApp flow. The existing public quote link remains the principal approval surface.

## Architecture

`VoltaMarcada.Application` owns these framework-neutral types:

- `IQuotePdfGenerator`, with `Task<byte[]> GenerateAsync(QuotePdfDocument document, CancellationToken cancellationToken)`;
- `QuotePdfDocument` and `QuotePdfLine`, immutable render-ready records;
- `GetQuotePdfUseCase` for authenticated company members;
- `GetPublicQuotePdfUseCase` for public-token access.

The use cases are responsible for tenant isolation, public-token validation, expiry checks, current quote status checks, safe data selection, and mapping domain data into `QuotePdfDocument`. They must not reference QuestPDF, ASP.NET Core response types, or a PDF library.

`VoltaMarcada.Infrastructure` references QuestPDF and supplies `QuestPdfQuotePdfGenerator`. It renders the passed document into PDF bytes. The generator contains layout code only. It does not query repositories or make authorization decisions.

`VoltaMarcada.Web` maps download endpoints and presents download actions. It converts successful PDF bytes into a file result, but it does not construct quote data, validate tenant ownership itself, or render PDFs.

## Document Content

The document contains only customer-facing commercial information:

- company trade name, legal name when available, document, phone/WhatsApp, city, state, and logo when available;
- document title and quote validity date;
- line description, item type, quantity, unit price, and line total;
- discount and final total;
- customer-facing notes.

The layout has a clear header, item table, total summary, and page numbers. It uses Brazilian Portuguese labels and Brazilian real currency formatting. Item types can be shown as localized labels but are not required as a separate table column when the layout becomes narrow.

The document must not contain aggregate IDs, user IDs, equipment IDs, service-request data, appointment information, internal history, raw public tokens, or internal notes. A missing or unavailable logo must result in a document without a logo, never in a failed PDF generation.

## Data and Authorization Flow

### Authenticated download

`GetQuotePdfUseCase` resolves the current company through the existing authorization/scope infrastructure. It loads the quote using both its ID and the resolved company ID. A missing or unauthorized quote returns the existing safe not-found/forbidden operation error. It maps the quote, its company, and its customer-facing company profile into `QuotePdfDocument`, then invokes the generator.

The authenticated route requires the existing company-member policy and uses the route quote ID only as a resource identifier. It never accepts `CompanyId` from the browser.

### Public download

`GetPublicQuotePdfUseCase` receives only the raw route token. It hashes the token before repository access and applies the same availability checks as `GetPublicQuoteUseCase`: the public token must exist, be unexpired and unrevoked, and the quote must be `Sent`, `Approved`, or `Rejected`. Expired quotes retain the existing status-transition behavior and are unavailable.

The public PDF endpoint uses a single safe unavailable response for absent, malformed, revoked, expired, and decided/unavailable links. It neither discloses why the request failed nor logs the raw token.

## HTTP and UX

Two GET endpoints return `File(bytes, "application/pdf", fileName)`:

- an authenticated quote PDF endpoint under the internal quote route family;
- `/q/{token}/pdf` for the public quote page.

File names use a safe human-readable prefix and quote identifier only where the identifier is already visible to the authorized internal user. Public filenames never contain the token or an internal identifier.

The internal quote registration page shows a secondary `Download PDF` action when viewing a persisted quote. The public quote page shows `Download PDF` whenever the public quote is available. Both links use normal browser download/navigation behavior, so no raw token is copied into application logs or client state beyond the existing route.

Generated files are not stored. Each download renders from the current authorized quote state. Drafts are available only through the authenticated route; public downloads retain the same availability conditions as the public quote view.

## Error Handling and Observability

Expected authorization and public-link states are returned as operation failures and mapped to safe Brazilian Portuguese messages. PDF rendering failures are unexpected errors: the web host returns a generic failure response and logs only technical diagnostics that exclude public tokens and unnecessary customer data.

The generator receives valid, complete document data. It may omit optional visual data such as a logo, but it must not silently omit required quote totals or items.

## Verification

Automated tests use a fake `IQuotePdfGenerator` and a deterministic `TimeProvider` to prove:

- authenticated lookup filters the quote by the current company;
- authenticated downloads may render drafts;
- public downloads hash tokens and allow only valid public quote states;
- expired, revoked, malformed, and unavailable public links have no document result;
- the application sends the expected customer-facing fields and totals to the generator;
- both HTTP endpoints return PDF content with the correct content type and safe filename behavior.

The implementation will also build the solution and run the relevant test projects, then inspect the produced PDF manually for readable header, item table, totals, and pagination.

## Dependencies

Infrastructure adds the QuestPDF NuGet package. QuestPDF is confined to the Infrastructure project. Application exposes only its own document records and generator abstraction, preserving the dependency direction required by the solution architecture.
