# Mobile Camera Reliability Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Make work-order camera capture resilient to mobile circuit suspension and preserve the execution tab after recovery.

**Architecture:** Replace circuit-mediated file bytes with a browser multipart upload to the existing authenticated photo endpoint. IndexedDB stores binary file data before upload; the page restores the execution tab from the URL after a reload. Circuit retention permits brief camera foreground transitions.

**Tech Stack:** .NET 10, ASP.NET Core Blazor Server, JavaScript, IndexedDB, xUnit.

**Spec:** docs/superpowers/specs/2026-09-03-mobile-camera-reliability-design.md

## Global Constraints

- Keep tenant authorization and server validation in existing photo endpoints.
- Do not send photo bytes or Base64 through Blazor Server interop.
- Preserve UI copy in Brazilian Portuguese and source-code names in English.
- Use apply_patch and test first.

### Task 1: Configure circuit retention and preserve execution tab

**Files:** Modify Program.cs, WorkOrderRegistration.razor, ReconnectModal.razor.js; create Components/MobileCameraReliabilityTests.cs.

- [ ] **Step 1: Write failing source tests**

~~~csharp
Assert.Contains("DisconnectedCircuitRetentionPeriod", ReadSource("Program.cs"));
Assert.Contains("ActivePanelIndex", ReadSource("Components/Pages/WorkOrderRegistration.razor"));
Assert.DoesNotContain("location.reload()", ReadSource("Components/Layout/ReconnectModal.razor.js"));
~~~

- [ ] **Step 2: Run and confirm RED**

Run: dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MobileCameraReliabilityTests

Expected: FAIL because configuration and tab persistence do not exist.

- [ ] **Step 3: Implement**

Configure CircuitOptions with five-minute DisconnectedCircuitRetentionPeriod and 1,000 MaximumRetainedDisconnectedCircuits. Bind MudTabs ActivePanelIndex to an execution-tab query value and update the query through NavigationManager without force reload. Replace unconditional rejected-circuit reload with a same-URL retry/reload path that retains the query string.

- [ ] **Step 4: Run and confirm GREEN**

Run: dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MobileCameraReliabilityTests

Expected: PASS.

### Task 2: Upload captured photos directly from browser HTTP

**Files:** Modify wwwroot/js/work-order-photo-staging.js and Components/Shared/WorkOrderExecutionPanel.razor; modify Components/MobileCameraReliabilityTests.cs.

- [ ] **Step 1: Write failing JavaScript-boundary tests**

~~~csharp
Assert.Contains("FormData", source);
Assert.Contains("credentials: \"same-origin\"", source);
Assert.DoesNotContain("Convert.ToBase64String", panelSource);
~~~

- [ ] **Step 2: Run and confirm RED**

Run: dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MobileCameraReliabilityTests

Expected: FAIL because uploads still cross the circuit and use Base64.

- [ ] **Step 3: Implement**

Store the captured File/Blob in IndexedDB before upload. Add a JavaScript upload function that builds FormData with file and photo type, sends the antiforgery token and same-origin credentials, parses operation errors, and clears staging only after a successful response. Make WorkOrderExecutionPanel invoke JavaScript without reading the browser file into .NET memory. Restore and retry staged records after render and visibility recovery.

- [ ] **Step 4: Run and confirm GREEN**

Run: dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~MobileCameraReliabilityTests

Expected: PASS.

### Task 3: Verify end-to-end behavior

- [ ] **Step 1: Run full verification**

Run: dotnet test VoltaMarcada.sln --no-restore

Expected: zero failed tests.

- [ ] **Step 2: Test on a physical mobile device**

Open an in-progress OS, select execution, capture a 5–10 MB photo, leave and return from the camera, simulate a temporary network interruption, and confirm the execution tab returns with either a persisted photo or a staged retry notice.

- [ ] **Step 3: Commit**

~~~powershell
git add src/VoltaMarcada.Web tests/VoltaMarcada.IntegrationTests
git commit -m "fix: preserve mobile work order photo capture"
~~~
