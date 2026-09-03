# Phase 9 Work Order Enhancements Design

## Goal

Complete the remaining Phase 9 enhancements by adding non-photo work-order attachments and an operator-oriented immutable work-order history. Existing before/after photos and public appointment-proposal confirmation remain in place and are not redesigned.

## Scope

This increment applies only to work orders.

It includes:

- attachments uploaded to a work order;
- history entries for meaningful work-order operations;
- a chronological history section on the authenticated work-order detail screen;
- tests for attachment behavior and the existing photo completion rules.

It excludes:

- reusable attachments on customers, equipment, appointments, or quotes;
- a generic polymorphic attachment model;
- a technical EF Core audit log;
- customer approval of execution reports or work-order completion;
- changes to the existing public appointment-proposal confirmation flow.

## Attachment Model

`WorkOrderAttachment` is a work-order-owned entity with `Id`, `CompanyId`, `WorkOrderId`, `FileKey`, `OriginalFileName`, `ContentType`, `Length`, optional `Description`, `UploadedByUserId`, and `CreatedAtUtc`.

The aggregate owns its attachments through a private collection and exposes behavior to add and remove them. Attachments are mutable only while the work order is in progress, matching the existing evidence-photo rule. Removal returns the entity so the application layer can delete the stored object after persistence succeeds.

The upload use case derives the company and authorization scope from the current user. It never accepts a company identifier from the request. It permits PDF and the existing image content types, rejects empty, invalid, or oversized uploads, and uses the existing `IFileStorage` abstraction under the tenant and work-order path. Photo uploads remain `WorkOrderPhoto`; attachments do not replace them.

The authenticated endpoints provide upload, download, and deletion. All three resolve the work order through the current company and professional access scope before accessing metadata or storage.

## Work-Order History

`WorkOrderHistoryEntry` is an immutable work-order-owned entity containing `Id`, `CompanyId`, `WorkOrderId`, `EventType`, optional `ActorUserId`, an operator-facing `Description`, and `CreatedAtUtc`.

`WorkOrderHistoryEventType` uses explicit domain values rather than free-form categories:

- `Created`
- `DetailsUpdated`
- `ProfessionalAssigned`
- `Started`
- `ChecklistProgressSaved`
- `ExecutionNotesUpdated`
- `PhotoAdded`
- `PhotoRemoved`
- `AttachmentAdded`
- `AttachmentRemoved`
- `AdditionalItemsUpdated`
- `Completed`
- `Cancelled`

The application use cases append entries inside the same persistence transaction/save operation as the corresponding aggregate mutation. The description is constructed in Application from stable, user-facing operation context; it must contain no raw storage key, token, sensitive document, or unbounded free text. No UI or API can edit or delete history entries.

Existing work orders do not receive synthetic entries. Their history begins with the first operation performed after this feature is deployed. Newly created work orders receive a `Created` entry.

## User Experience

The work-order detail UI keeps before/after photos in the execution panel. It adds a separate attachment section for PDF and image documents with upload, download, description, and removal while execution is allowed. The interface clearly distinguishes attachments from execution photos.

The detail page also shows a chronological `Histórico da OS` section. Each item shows a concise Portuguese description, actor when available, and a timezone-formatted date/time. It starts with a useful empty state for work orders that have no post-deployment events.

## Security and Validation

- Every attachment and history query filters by `CompanyId` and authorized work-order access.
- The browser supplies neither a tenant identity nor an authorization decision.
- Uploads accept only PDF, JPEG, PNG, WebP, and HEIC; length limits are enforced server-side.
- Storage cleanup follows the established photo pattern: delete a newly uploaded object if metadata persistence fails, and delete removed objects after metadata persistence succeeds.
- History descriptions are bounded and generated server-side. They do not expose storage keys, raw public tokens, internal IDs, or customer-sensitive data.

## Persistence

EF Core configurations and a descriptive migration add `WorkOrderAttachments` and `WorkOrderHistoryEntries`, with foreign keys to work orders and indexes supporting `(CompanyId, WorkOrderId, CreatedAtUtc)`. The DbContext exposes both sets. PostgreSQL naming continues to follow the repository's current convention.

## Testing

Tests use xUnit and deterministic `TimeProvider` values. They cover:

- attachment domain invariants and immutable history entries;
- attachment upload validation, current-company/professional authorization, storage cleanup, and removal;
- work-order completion requiring configured before/after evidence;
- history creation for new work orders and representative state-changing operations;
- UI structural coverage for attachment and history sections where the repository uses component-source tests.

## Acceptance Criteria

A permitted internal user can upload, view, download, and remove a PDF or supported image attachment only for an in-progress work order within their company. Each successful work-order operation creates an immutable, concise chronology item visible in the detail UI. The existing photo workflow continues to require before/after photos when configured, and public appointment proposal confirmation remains unchanged.
