# Phase 13 VPS Landing Page and Production Deployment Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Publish VoltaMarcada safely and reproducibly on a VPS with a public landing page at `voltamarcada.com.br` and the application at `app.voltamarcada.com.br`.

**Architecture:** Keep the Blazor application as one production container and serve a static landing page in a separate container. Docker Compose coordinates the application, PostgreSQL, static page, and reverse proxy; the reverse proxy terminates HTTPS and routes by hostname. Production configuration comes exclusively from VPS environment/secrets.

**Tech Stack:** Docker, Docker Compose, Caddy, PostgreSQL 17, .NET 10 ASP.NET Core, static HTML/CSS.

**Spec:** `VoltaMarcada_Plano_Negocio_Desenvolvimento_v2.md` (Phase 13).

## Global Constraints

- The existing root `Dockerfile` is a development baseline, not the final VPS deployment contract.
- Do not commit passwords, connection strings, Cloudinary credentials, PIX keys, SMTP credentials, or TLS private keys.
- PostgreSQL is not exposed publicly from the VPS Compose network.
- HTTPS is mandatory for both domains; forwarded headers, secure cookies, and application base URLs must match the final public hosts.
- Deployment and rollback must use immutable image tags and an explicit backup/restore procedure.

---

## File Structure

- Create `deploy/Dockerfile.web` and `deploy/Dockerfile.landing` as production images.
- Create `deploy/compose.production.yml`, `deploy/Caddyfile`, `.env.production.example`, `deploy/backup-postgres.ps1`, and `deploy/restore-postgres.ps1`.
- Create `landing/index.html`, `landing/styles.css`, and static assets for the first public marketing page.
- Modify `src/VoltaMarcada.Web/appsettings.json` only for safe production defaults; never place real values in it.
- Create `docs/operations/vps-deployment.md` as the operator runbook.

### Task 1: Define production configuration and container boundaries

**Files:**
- Create: `deploy/Dockerfile.web`, `deploy/Dockerfile.landing`, `deploy/compose.production.yml`, `.env.production.example`
- Modify: `.dockerignore`, `src/VoltaMarcada.Web/appsettings.json`
- Test: Compose configuration validation

- [ ] **Step 1: Write the configuration acceptance checklist**

Document required non-secret variables: application image tag, PostgreSQL database/user/password, connection string, application/landing URLs, platform-admin bootstrap settings, Cloudinary settings, licensing settings, and SMTP settings. The example file contains placeholders only.

- [ ] **Step 2: Build production images**

Make the Web image multi-stage and run as a non-root runtime user where compatible with ASP.NET Core file requirements. Make the landing image static-only. Compose connects Web and PostgreSQL on a private network, gives PostgreSQL a named volume, and never declares a host port for PostgreSQL.

- [ ] **Step 3: Validate the declarative configuration**

Run: `docker compose --env-file .env.production.example -f deploy/compose.production.yml config`

Expected: configuration resolves without embedded real secrets and exposes only 80/443 through the reverse proxy.

### Task 2: Add HTTPS routing, health, persistence, and recoverability

**Files:**
- Create: `deploy/Caddyfile`, `deploy/backup-postgres.ps1`, `deploy/restore-postgres.ps1`
- Modify: `deploy/compose.production.yml`
- Test: container health and backup/restore rehearsal

- [ ] **Step 1: Configure hostname routing**

Route `voltamarcada.com.br` to the landing container and `app.voltamarcada.com.br` to the Web container. Caddy obtains and renews certificates, redirects HTTP to HTTPS, and forwards the host/protocol headers expected by ASP.NET Core.

- [ ] **Step 2: Add observable health checks**

Add container health checks for PostgreSQL, Web, landing page, and proxy. Web health must avoid exposing customer data or secrets. Configure restart policies and persistent volumes for PostgreSQL and Caddy certificate state.

- [ ] **Step 3: Implement backup and restore commands**

The backup script runs `pg_dump` inside the production database container, names the encrypted/off-host artifact with a UTC timestamp, and returns non-zero on failure. The restore script requires an explicit backup path and target confirmation, then documents the expected maintenance window. Rehearse restoration against a disposable database before launch.

### Task 3: Create the first landing page and login handoff

**Files:**
- Create: `landing/index.html`, `landing/styles.css`
- Test: `tests/VoltaMarcada.IntegrationTests/Components/LandingPageSourceTests.cs` or static source verification

- [ ] **Step 1: Write the static source test**

Assert that the page has a Portuguese value proposition focused on operational organization and maintenance recurrence, an accessible primary call-to-action, and a “Já tenho conta” action to `https://app.voltamarcada.com.br/account/login`.

- [ ] **Step 2: Implement a responsive static page**

Use semantic HTML, responsive CSS, no tracking SDK, and no user data collection. Keep the page focused on the problem, maintenance-return outcome, product flow, and account/login calls to action.

- [ ] **Step 3: Validate links and rendering**

Build the landing image, request it through the proxy in a local production-like stack, and verify redirects/links preserve HTTPS and do not leak development addresses.

### Task 4: Document VPS provisioning, release, rollback, and verification

**Files:**
- Create: `docs/operations/vps-deployment.md`
- Test: runbook checklist review

- [ ] **Step 1: Document VPS prerequisites**

Specify Ubuntu/Docker installation, firewall ports 80/443 only, DNS A/AAAA records for both domains, non-root deploy user, secret-file permissions, storage sizing, and PostgreSQL backup destination.

- [ ] **Step 2: Document safe release and rollback**

Define: pull immutable image tag, validate Compose, take backup, run `docker compose up -d`, inspect health/logs, verify browser flows, and roll back to the previous explicit tag when verification fails. Do not use `latest` for production releases.

- [ ] **Step 3: Perform pre-launch verification and commit**

Run Compose config validation, image builds, `git diff --check`, and a documented backup/restore rehearsal. Commit with `chore: add vps deployment and landing page infrastructure`.