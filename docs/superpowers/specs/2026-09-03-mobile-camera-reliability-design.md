# Mobile Camera Reliability Design

## Goal

Keep a mobile user on the work-order execution tab when returning from the device camera, prevent captured evidence from depending on the Blazor Server circuit, and recover safely after temporary connectivity loss.

## Root Cause

The interactive server circuit can be suspended while the native camera takes foreground. The current reconnect script reloads the page when the circuit cannot resume, and WorkOrderRegistration always initializes MudTabs at its first panel. The current photo flow also transfers image bytes through the circuit and Base64 JS interop after capture, making it vulnerable to circuit limits and mobile connection loss.

## Design

Photos upload from browser JavaScript with multipart/form-data to the existing authenticated work-order photo endpoint. The request uses same-origin credentials and antiforgery protection. Image bytes no longer traverse the Blazor circuit or Base64 JS interop.

Before attempting the HTTP upload, JavaScript stores the returned File object and metadata in IndexedDB. A successful upload removes the staged item. A failed upload leaves it staged and the execution panel asks JavaScript to retry when the circuit reconnects or the document becomes visible.

The execution tab is represented by a query parameter on the work-order registration route. Tab changes update that parameter without forcing navigation. On a circuit-resume or browser reload, the page reads the parameter and renders the execution tab. The reconnect script records the current URL before a reload and reloads that same URL, never an application root.

Server circuit options retain disconnected circuits for five minutes and retain up to 1,000 disconnected circuits. This is a recovery window, not authorization; all photo HTTP endpoints continue to enforce tenant and professional access.

## Scope

- Work-order evidence photos only.
- Direct browser HTTP upload with existing endpoint and cookie authentication.
- IndexedDB staging using binary File/Blob data rather than Base64.
- Execution-tab restoration after reconnect/reload.
- Server disconnected-circuit retention configuration.
- Tests for JavaScript boundary, tab URL state, and server configuration.

## Non-Goals

- Direct browser uploads for generic work-order attachments.
- Changing Cloudinary implementation, file types, sizes, or authorization rules.
- Guaranteeing that a mobile browser never suspends its network connection.
- Persisting unsaved form fields outside the execution-photo workflow.

## Security

The photo endpoint remains authenticated and company-scoped. JavaScript obtains an antiforgery token from the existing page form/token mechanism and sends it with every POST. The browser never receives a storage credential or raw Cloudinary token.

## Acceptance Criteria

After choosing or capturing a photo on mobile, the user returns to the execution tab. If the circuit disconnects, the current OS URL and execution tab are restored after recovery. A photo captured before an upload failure remains locally staged and can be retried. Successful uploads are persisted through the existing endpoint and removed from local staging.
