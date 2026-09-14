# Phase 13 Landing Page and VPS Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (- [ ]) syntax for tracking.

**Goal:** Deliver a fast public landing page and a reproducible, secure Linux VPS deployment for VoltaMarcada.

**Architecture:** A static, framework-free landing page runs in its own container at voltamarcada.com.br. The existing Blazor application runs separately at app.voltamarcada.com.br; Docker Compose places both behind Caddy with a private PostgreSQL container.

**Tech Stack:** .NET 10, ASP.NET Core, xUnit, Docker, Docker Compose, Caddy, PostgreSQL 17, semantic HTML, CSS, and minimal JavaScript.

**Spec:** docs/superpowers/specs/2026-09-14-phase-thirteen-vps-landing-page-and-deployment-design.md

## Global Constraints

- The landing is static, mobile-first, accessible, tracking-free, and uses no frontend framework.
- Primary conversion links to https://app.voltamarcada.com.br/account/register; login links to https://app.voltamarcada.com.br/account/login.
- Only Caddy exposes ports 80/443. PostgreSQL remains private to Compose.
- Production secrets are never committed. .NET environment variables supply them.
- Production image tags are immutable. The Web runtime runs as non-root.
- Tests must not need live DNS, a purchased VPS, or external services.

---

## File Structure

- Create: deploy/Dockerfile.web — multi-stage, non-root .NET production image.
- Create: deploy/Dockerfile.landing — non-root static-file image.
- Create: deploy/compose.production.yml — Caddy, landing, Web, and private PostgreSQL topology.
- Create: deploy/Caddyfile — hostname routes and HTTPS proxy configuration.
- Create: deploy/.env.production.example — variable names and non-secret values only.
- Create: deploy/backup-postgres.sh and deploy/restore-postgres.sh — guarded Linux operations.
- Create: landing/index.html, landing/styles.css, landing/landing.js, landing/assets/ — public page and replaceable media.
- Create: tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs — deployment source contracts.
- Create: tests/VoltaMarcada.IntegrationTests/Components/LandingPageSourceTests.cs — landing source contracts.
- Modify: src/VoltaMarcada.Web/Program.cs, src/VoltaMarcada.Web/appsettings.json, .dockerignore, README.md.
- Create: docs/operations/vps-deployment.md — operator handoff.

### Task 1: Establish safe configuration and a health endpoint

**Files:**
- Create: tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs
- Modify: src/VoltaMarcada.Web/Program.cs
- Modify: src/VoltaMarcada.Web/appsettings.json
- Modify: .dockerignore

**Interfaces:**
- Produces: anonymous GET /health with HTTP 200 and body Healthy.
- Produces: no committed database connection, Cloudinary secret, SMTP password, or billing PIX key.

- [ ] **Step 1: Write the failing source-contract test**

Create an xUnit source test which reads Program.cs and parses appsettings.json with JsonDocument:

~~~csharp
Assert.Contains("app.MapGet(\"/health\"", program);
Assert.Contains("Results.Text(\"Healthy\")", program);
Assert.Equal(string.Empty, root.GetProperty("ConnectionStrings").GetProperty("DefaultConnection").GetString());
Assert.Equal(string.Empty, root.GetProperty("Cloudinary").GetProperty("ApiSecret").GetString());
Assert.Equal(string.Empty, root.GetProperty("Smtp").GetProperty("Password").GetString());
Assert.Equal(string.Empty, root.GetProperty("LicenseBilling").GetProperty("PixKey").GetString());
~~~

- [ ] **Step 2: Run test to verify it fails**

Run:

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~ProductionDeploymentSourceTests
~~~

Expected: FAIL because health is unmapped and a billing key is committed.

- [ ] **Step 3: Implement the configuration boundary**

Map this endpoint after middleware setup and before Razor component mapping:

~~~csharp
app.MapGet("/health", () => Results.Text("Healthy"));
~~~

Set LicenseBilling:PixKey to an empty string in versioned settings. Keep current validation; user secrets and production environment variables supply the value. Add deploy/.env.production and deploy/backups to .dockerignore.

- [ ] **Step 4: Run verification**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~ProductionDeploymentSourceTests
dotnet build VoltaMarcada.sln
~~~

Expected: both commands exit with code 0.

- [ ] **Step 5: Commit**

~~~powershell
git add .dockerignore src/VoltaMarcada.Web/Program.cs src/VoltaMarcada.Web/appsettings.json tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs
git commit -m "feat: add production health endpoint"
~~~

### Task 2: Add production images and Compose topology

**Files:**
- Create: deploy/Dockerfile.web
- Create: deploy/Dockerfile.landing
- Create: deploy/compose.production.yml
- Create: deploy/.env.production.example
- Modify: tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs

**Interfaces:**
- Consumes: GET /health from Task 1.
- Produces: Compose services caddy, landing, web, and postgres.
- Produces: docker compose --env-file deploy/.env.production.example -f deploy/compose.production.yml config.

- [ ] **Step 1: Extend the failing topology test**

Assert all four services, Caddy mappings "80:80" and "443:443", Web health URL http://localhost:8080/health, pg_isready, and USER app in the Web Dockerfile. Assert the PostgreSQL service has no ports block.

- [ ] **Step 2: Run test to verify it fails**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~ProductionDeploymentSourceTests
~~~

Expected: FAIL because deploy artifacts do not exist.

- [ ] **Step 3: Implement the Web image**

Create deploy/Dockerfile.web using SDK 10.0 restore/publish stages and runtime mcr.microsoft.com/dotnet/aspnet:10.0. Publish src/VoltaMarcada.Web/VoltaMarcada.Web.csproj to /app/publish. Set ASPNETCORE_URLS=http://+:8080, copy output with app ownership, set USER app, expose 8080, and run VoltaMarcada.Web.dll.

- [ ] **Step 4: Implement landing image and Compose**

Create deploy/Dockerfile.landing from nginx:1.27-alpine, copy landing into the document root, and listen on 8080 without root privileges.

Compose uses named volumes postgres-data, caddy-data, caddy-config; an edge network for Caddy/landing/Web; and a private backend network for Web/PostgreSQL. All services use restart: unless-stopped and health checks. PostgreSQL has no host port. The example environment file contains no credentials and lists PostgreSQL, URL, Cloudinary, SMTP, licensing, and platform-bootstrap variables. It sets WEB_IMAGE=voltamarcada-web:0.0.0 and LANDING_IMAGE=voltamarcada-landing:0.0.0.

- [ ] **Step 5: Validate images and configuration**

~~~powershell
docker compose --env-file deploy/.env.production.example -f deploy/compose.production.yml config
docker build -f deploy/Dockerfile.web -t voltamarcada-web:phase13-test .
docker build -f deploy/Dockerfile.landing -t voltamarcada-landing:phase13-test .
~~~

Expected: all exit with code 0; only Caddy publishes ports.

- [ ] **Step 6: Commit**

~~~powershell
git add deploy/Dockerfile.web deploy/Dockerfile.landing deploy/compose.production.yml deploy/.env.production.example tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs
git commit -m "feat: add production container topology"
~~~

### Task 3: Configure HTTPS, health checks, and recovery

**Files:**
- Create: deploy/Caddyfile
- Create: deploy/backup-postgres.sh
- Create: deploy/restore-postgres.sh
- Create: docs/operations/vps-deployment.md
- Modify: deploy/compose.production.yml
- Modify: tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs

**Interfaces:**
- Consumes: service names from Task 2.
- Produces: ./deploy/backup-postgres.sh and ./deploy/restore-postgres.sh /absolute/path/backup.dump.

- [ ] **Step 1: Write failing routing and script safety tests**

Require both hostnames plus reverse_proxy landing:8080 and reverse_proxy web:8080 in Caddyfile. Require set -euo pipefail, pg_dump, and a UTC timestamp expression in backup. Require exact RESTORE confirmation and pg_restore --clean --if-exists --no-owner in restore.

- [ ] **Step 2: Run test to verify it fails**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~ProductionDeploymentSourceTests
~~~

Expected: FAIL because routing and scripts are missing.

- [ ] **Step 3: Implement Caddy and health checks**

Create Caddy blocks for voltamarcada.com.br to landing:8080 and app.voltamarcada.com.br to web:8080. Enable encode zstd gzip. Do not configure insecure TLS or certificate paths. Compose health checks use Caddy local admin, wget http://localhost:8080/ for landing, http://localhost:8080/health for Web, and pg_isready for PostgreSQL.

- [ ] **Step 4: Implement backup and guarded restore**

Backup defaults to /var/backups/voltamarcada, creates the directory with restrictive permissions, executes custom-format pg_dump inside postgres, writes voltamarcada-YYYYMMDDTHHMMSSZ.dump, and prints its absolute path after success.

Restore requires exactly one existing absolute dump, warns of overwrite, requires literal RESTORE, stops Web, runs pg_restore --clean --if-exists --no-owner in postgres, and starts Web only on success. Restore failure leaves Web stopped. Mark both scripts executable using git update-index --chmod=+x.

- [ ] **Step 5: Write operator runbook**

Document Ubuntu LTS, non-root deploy user in docker group, Docker Engine and Compose plugin, DNS A/AAAA records, firewall TCP 80/443, secret file mode 600, persistent disk, and off-host backup.

Release order: select immutable tags; validate Compose; backup; Compose up; inspect health/logs; verify both HTTPS hosts and account links; retain old tags; roll back with old explicit tags. Include a restore rehearsal against a disposable database.

- [ ] **Step 6: Run verification**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~ProductionDeploymentSourceTests
docker compose --env-file deploy/.env.production.example -f deploy/compose.production.yml config
docker run --rm -v "$PWD/deploy/Caddyfile:/etc/caddy/Caddyfile:ro" caddy:2.8-alpine caddy validate --config /etc/caddy/Caddyfile --adapter caddyfile
~~~

Expected: all exit with code 0.

- [ ] **Step 7: Commit**

~~~powershell
git add deploy/Caddyfile deploy/backup-postgres.sh deploy/restore-postgres.sh deploy/compose.production.yml docs/operations/vps-deployment.md tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs
git commit -m "feat: add vps routing and recovery"
~~~

### Task 4: Build the public landing page

**Files:**
- Create: landing/index.html
- Create: landing/styles.css
- Create: landing/landing.js
- Create: landing/assets/README.md
- Create: landing/assets/favicon.svg
- Create: landing/assets/og-image.svg
- Create: tests/VoltaMarcada.IntegrationTests/Components/LandingPageSourceTests.cs

**Interfaces:**
- Produces: anchors #inicio, #como-funciona, #recursos, and #preco.
- Produces: replaceable dashboard, maintenance, public-page, schedule, work-order, and quote media slots.

- [ ] **Step 1: Write failing landing source-contract tests**

Read UTF-8 landing files. Require exact hero phrase, a single h1, main, all required anchors, both account URLs, R$ 49,90, 14 dias grátis, FAQ details, viewport/description/Open Graph metadata, prefers-reduced-motion, and no localhost or 127.0.0.1.

~~~csharp
Assert.Contains("<h1>O serviço termina. O cliente não.</h1>", html);
Assert.Contains("https://app.voltamarcada.com.br/account/register", html);
Assert.Contains("https://app.voltamarcada.com.br/account/login", html);
Assert.Equal(1, Regex.Matches(html, "<h1").Count);
Assert.Contains("id=\"como-funciona\"", html);
Assert.DoesNotContain("localhost", html, StringComparison.OrdinalIgnoreCase);
~~~

- [ ] **Step 2: Run test to verify it fails**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~LandingPageSourceTests
~~~

Expected: FAIL because landing files do not exist.

- [ ] **Step 3: Implement approved content and semantics**

Use lang=pt-BR, UTF-8, canonical https://voltamarcada.com.br, the approved title/description, Open Graph, and favicon. Implement: sticky navigation; hero; three problem cards; six-step flow; public-page mobile section; six feature cards with recurrence emphasized; navy recurrence section; connected workflow/context; one-price offer; FAQ; final CTA; footer. Use links for navigation/conversion, a button only for mobile menu, and native details/summary for FAQ.

- [ ] **Step 4: Implement responsive styling and minimal JavaScript**

Use CSS variables for blue/light-blue/white/navy, visible focus, responsive frames, a desktop two-column/mobile one-column hero, desktop horizontal/mobile vertical flow, and useful media crops. In prefers-reduced-motion set transitions and animations to zero. JavaScript toggles aria-expanded on the mobile menu, closes it on internal navigation, and uses IntersectionObserver only when available to add is-visible on data-reveal elements.

- [ ] **Step 5: Document replaceable visuals**

Write landing/assets/README.md with these exact assets and require synthetic data only:

~~~text
hero-dashboard.webp — 1600x1000 — populated dashboard
maintenance-reminders.webp — 1440x960 — maintenance dashboard
public-request-mobile.webp — 750x1334 — public request page
schedule.webp — 1440x960 — populated schedule
work-order.webp — 1440x960 — filled work order
quote.webp — 1440x960 — optional filled quote
~~~

- [ ] **Step 6: Verify source and container behavior**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~LandingPageSourceTests
docker build -f deploy/Dockerfile.landing -t voltamarcada-landing:phase13-test .
docker run --rm -d --name voltamarcada-landing-test -p 18080:8080 voltamarcada-landing:phase13-test
Invoke-WebRequest http://localhost:18080/ -UseBasicParsing | Select-Object -ExpandProperty StatusCode
docker rm -f voltamarcada-landing-test
~~~

Expected: test passes and HTTP status is 200.

- [ ] **Step 7: Commit**

~~~powershell
git add landing tests/VoltaMarcada.IntegrationTests/Components/LandingPageSourceTests.cs
git commit -m "feat: add public landing page"
~~~

### Task 5: Complete local production-like verification and handoff

**Files:**
- Modify: docs/operations/vps-deployment.md
- Modify: README.md
- Modify: tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs

**Interfaces:**
- Consumes: all artifacts from Tasks 1-4.
- Produces: an operator checklist usable before a VPS is purchased.

- [ ] **Step 1: Add a failing documentation contract**

Assert the runbook contains docker compose, backup-postgres.sh, restore-postgres.sh, both hostnames, 80/443, and immutable. Assert README links docs/operations/vps-deployment.md and identifies root docker-compose.yml as development-only.

- [ ] **Step 2: Run test to verify it fails**

~~~powershell
dotnet test tests/VoltaMarcada.IntegrationTests --filter FullyQualifiedName~ProductionDeploymentSourceTests
~~~

Expected: FAIL until README differentiates the development database Compose file from production.

- [ ] **Step 3: Add local acceptance instructions**

Document a disposable local environment file and optional temporary hosts mappings. Verify both host routes, /health, registration/login links, health status, and logs. Local Caddy certificates can be untrusted; production must never use TLS bypasses. Add a concise README link to the runbook and mark root docker-compose.yml as local PostgreSQL development only.

- [ ] **Step 4: Run full verification**

~~~powershell
dotnet test VoltaMarcada.sln
dotnet build VoltaMarcada.sln
docker compose --env-file deploy/.env.production.example -f deploy/compose.production.yml config
git diff --check
~~~

When Docker is available, run the documented production-like stack. If unavailable, report it as not run and do not claim a stack verification.

- [ ] **Step 5: Commit**

~~~powershell
git add README.md docs/operations/vps-deployment.md tests/VoltaMarcada.IntegrationTests/Deployment/ProductionDeploymentSourceTests.cs
git commit -m "docs: add phase thirteen deployment handoff"
~~~

## Plan Self-Review

- Separate hosts, direct registration, price/trial, narrative, accessibility, SEO, mobile UX, and replaceable media: Task 4.
- Private database, non-root runtime, HTTPS, volumes, health, secrets, immutable tags, recovery, and Linux operations: Tasks 1-3.
- Pre-VPS validation and operator handoff: Tasks 2, 4, and 5.
- Hostnames, service names, health endpoint, and application URLs are consistent across tasks.

