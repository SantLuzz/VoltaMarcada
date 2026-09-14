# Phase 13: Landing Page and VPS Deployment Design

## Goal

Publish VoltaMarcada through a reproducible Linux VPS deployment while making
the public value proposition clear in the first seconds of a visit:

> The service ends. The customer does not.

The public landing page converts visitors directly into self-service account
creation. The authenticated product remains at a separate hostname.

## Decisions

- `voltamarcada.com.br` serves a static marketing landing page.
- `app.voltamarcada.com.br` serves the authenticated Blazor application.
- The landing page stays in this repository but is not a second .NET project.
  It is an independent static site and production container.
- Docker Compose is the initial production orchestrator on a Linux VPS.
- Caddy is the public reverse proxy and terminates HTTPS for both hostnames.
- PostgreSQL is private to the Compose network and has no host port.
- Production configuration and secrets exist only on the VPS, never in Git.
- The initial commercial offer is one plan: R$ 49,90 per month, with a
  14-day free trial and no card required to start.

## Audience and Positioning

The first audience is Brazilian companies that provide air-conditioning and
recurring maintenance services: installation, cleaning, hygiene, preventive
maintenance, and corrective maintenance. Copy uses this context to be
concrete, while remaining useful for other recurring service businesses.

VoltaMarcada is not positioned merely as work-order software. Its differentiator
is the recurrence loop:

```text
Completed service -> next maintenance recorded -> company reminded -> future opportunity
```

The landing page must make this benefit clear before presenting product features.

## Landing Page Experience

The page is a responsive, mobile-first static page. Its visual language follows
the current VoltaMarcada identity: blue, light blue, white, and restrained navy
sections; generous whitespace; strong typography; rounded cards; and subtle
shadows. It avoids generic-SaaS visuals, excessive gradients, abstract
illustrations, tracking SDKs, and continuous or distracting animations.

The navbar is sticky and contains the VoltaMarcada logo, links to page sections,
the application login, and the primary `Começar grátis` action. On mobile it
becomes an accessible hamburger menu.

All primary conversion actions link to:

```text
https://app.voltamarcada.com.br/account/register
```

The existing-account action links to:

```text
https://app.voltamarcada.com.br/account/login
```

### Narrative Order

1. **Hero:** `O serviço termina. O cliente não.` The subheadline explains that
   VoltaMarcada organizes requests, quotes, schedules, and work orders while
   showing the right time to contact customers for their next maintenance.
   It has primary and secondary calls to action, three concise confidence
   points, and a browser-frame product preview.
2. **Problem:** explains forgotten customers, overdue maintenance, and lost
   revenue, ending with the statement that completed services become future
   opportunities.
3. **How it works:** presents the product flow from customer request through
   scheduling, quote, work order, next maintenance, and reminder. It is
   horizontal on larger screens and vertical on mobile.
4. **Public page:** shows a mobile-frame preview and explains that a customer
   can request service without installing an app.
5. **Features:** contains six focused cards: schedule, work orders, quotes,
   customers and equipment, checklists and evidence, and upcoming maintenance.
   The upcoming-maintenance card receives stronger emphasis.
6. **Recurrence:** a high-contrast navy section shows maintenance opportunities
   and reinforces that the next customer may be someone already served.
7. **Connected workflow and usage context:** shows the relationship among
   requests, quotes, service, history, and next maintenance, then ties the
   product to companies that live from field service and maintenance.
8. **Price:** one simple plan at R$ 49,90/month, its included essentials,
   `14 dias grátis`, and no-card message.
9. **FAQ, final CTA, and footer:** answers the most common product questions,
   repeats the self-service conversion path, and reserves footer links for
   future terms, privacy, and contact pages.

## Media and Motion

The product is the visual protagonist. The first release uses clearly isolated
demonstrative media inside browser and mobile frames, which can be replaced
without changing the document structure when real screenshots are prepared.
The planned real captures are dashboard, maintenance reminders, public request
page, schedule, work order, and optionally quote. They use synthetic data only.

A realistic Brazilian technician image may be used only in the lower usage
context section, never as the hero visual. Motion is limited to reduced-motion
safe fade or short entrance transitions, small hover feedback, and restrained
mockup movement.

## Accessibility, SEO, and Performance

- Use semantic landmarks, correct heading hierarchy, visible keyboard focus,
  accessible menu behavior, sufficient contrast, and `prefers-reduced-motion`.
- Keep the page framework-free, with CSS and JavaScript only where necessary.
- Use responsive images, explicit dimensions, modern compressed formats, and
  lazy loading for media below the initial viewport.
- Add a Portuguese page title, meta description, canonical URL, Open Graph
  metadata, favicon, and logo.
- Do not collect visitor data or add third-party tracking in this phase.

Suggested metadata:

```text
Title: VoltaMarcada | Gestão de serviços e manutenções recorrentes
Description: Organize solicitações, agenda, orçamentos e ordens de serviço e saiba quando chamar seus clientes novamente para a próxima manutenção.
```

## Production Architecture

```text
Internet
  -> Caddy (ports 80 and 443, HTTPS and hostname routing)
       -> Landing container (voltamarcada.com.br)
       -> Web container (app.voltamarcada.com.br)
            -> PostgreSQL container (private network only)
```

The deployment adds production-specific Dockerfiles for the Web application and
landing page. The Web image uses a multi-stage .NET build and a non-root runtime
user where compatible with ASP.NET Core requirements. The landing image serves
only static files.

Docker Compose defines private service networking, restart policies, named
volumes for PostgreSQL and Caddy certificate state, and health checks for every
service. The Web health endpoint exposes no customer data or secrets.

## Configuration and Operations

An example environment file documents required variable names and placeholders
only. The VPS holds actual values with restrictive file permissions. Configuration
includes public application and landing URLs, database credentials and connection
string, Cloudinary, SMTP, licensing, platform administrator bootstrap settings,
and immutable release image tags.

The operator runbook covers Ubuntu and Docker prerequisites, non-root deployment
user, DNS records, firewall ports 80/443, storage planning, external backup
destination, release verification, and explicit rollback to a prior image tag.
It never uses `latest` as a production release reference.

Backup creates a timestamped PostgreSQL dump and returns a failure status when
the command fails. Restore requires an explicit backup path and operator target
confirmation. Restore is rehearsed against a disposable database before launch.

## Verification

Before a VPS is acquired, verify locally that the production Compose file
resolves from example configuration, production images build, container health
checks pass, proxy routing works in a production-like stack, and no development
URLs appear in public links. Validate static source expectations for the key
Portuguese proposition, registration/login destinations, accessibility, and SEO
metadata. Perform a documented backup and restore rehearsal before launch.

## Scope Boundary

This phase prepares a reliable first production operation and landing page. It
does not add a CMS, blog, analytics platform, payment processor, new frontend
framework, or a more complex hosting platform.
