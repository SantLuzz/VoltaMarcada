# Landing Recurrence Redesign Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Rebuild the landing around recurring maintenance, use the supplied VoltaMarcada logo, and publish the static landing on Render.

**Architecture:** The page stays dependency-free HTML, CSS, and JavaScript. A Render Blueprint deploys `deploy/Dockerfile.landing` as a public Docker web service; Nginx reads Render's `PORT` variable through its image-template mechanism.

**Tech Stack:** HTML5, CSS, vanilla JavaScript, Nginx 1.27 Alpine, Render Blueprint.

**Spec:** `docs/superpowers/specs/2026-09-14-landing-recurrence-redesign-design.md`

## Global Constraints

- Preserve R$ 49,90/month, 14 free days, and no-credit-card terms.
- Do not promise automatic confirmation, WhatsApp API automation, online payments, native apps, routing, or unsupported modules.
- Use the supplied 32×32 favicon as published landing branding.
- Product screenshots must use synthetic data and retain CSS fallbacks when absent.
- Keep keyboard focus, no-JavaScript behavior, reduced motion, and no third-party dependency.

---

### Task 1: Add brand asset and recurrence-first content

**Files:**

- Create: `landing/assets/logo.png`
- Modify: `landing/index.html`
- Modify: `landing/assets/README.md`

**Interfaces:**

- Consumes: `src/VoltaMarcada.Web/wwwroot/icons/favicon-32x32.png`.
- Produces: `assets/logo.png`, `#inicio`, `#como-funciona`, `#recursos`, `#preco`, `.recurrence-cycle`, `.proof-section`, `.operation-path`, and `.whatsapp-path`.

- [ ] **Step 1: Copy the supplied 32×32 logo**

```powershell
Copy-Item -LiteralPath 'src\VoltaMarcada.Web\wwwroot\icons\favicon-32x32.png' -Destination 'landing\assets\logo.png'
```

- [ ] **Step 2: Replace letter marks and SVG favicon use**

```html
<link rel="icon" href="assets/logo.png" type="image/png">
<a class="brand" href="#inicio" aria-label="VoltaMarcada, início"><img class="brand-logo" src="assets/logo.png" alt="">Volta<span>Marcada</span></a>
```

- [ ] **Step 3: Replace generic messaging with approved story**

Use “Seu cliente vai precisar de manutenção novamente.” in the hero, “Quantos clientes você já atendeu e nunca chamou novamente?” as pain heading, and this conclusion:

```html
<p class="problem-statement">Serviço concluído <span aria-hidden="true">≠</span> relacionamento encerrado.</p>
```

- [ ] **Step 4: Build the six-step cycle and product proof**

Create, in order, Atendimento, Ordem de serviço, Serviço concluído, Próxima manutenção, Contato com o cliente, and Novo atendimento. Follow with distinct maintenance-panel, equipment-history, public-request-phone, and work-order sections. Below-fold images use `loading="lazy"`, explicit dimensions, and the existing `onerror` fallback.

- [ ] **Step 5: Add the compact operation and WhatsApp flow**

Use `Solicitação → Agenda → Orçamento → Aprovação → Ordem de serviço → Execução → Histórico → Próxima manutenção`; make clear the company confirms the public request and continues communicating through WhatsApp.

- [ ] **Step 6: Preserve conversion and update asset handoff**

Keep pricing, trial, FAQ, registration/login links, and final CTA “O serviço termina. O cliente não.” Document `logo.png` and expected synthetic-data screenshot files in `landing/assets/README.md`.

- [ ] **Step 7: Validate and commit**

```powershell
rg -n 'logo.png|Seu cliente vai precisar|Quantos clientes|R\$ 49|14 dias grátis|automaticamente' landing/index.html landing/assets/README.md
git add landing/index.html landing/assets/logo.png landing/assets/README.md
git commit -m "feat: refocus landing on maintenance recurrence"
```

### Task 2: Implement visual hierarchy and accessible responsiveness

**Files:**

- Modify: `landing/styles.css`

**Interfaces:**

- Consumes: classes from Task 1.
- Produces: responsive hero, cycle, proof, operation, and logo layouts.

- [ ] **Step 1: Add brand-image and reusable visual tokens**

```css
:root { --surface-soft: #f8fbfe; --accent-soft: #eaf5ff; --shadow-raised: 0 1.5rem 4rem rgba(11, 37, 84, .14); }
.brand-logo { width: 1.7rem; height: 1.7rem; flex: 0 0 auto; object-fit: contain; }
```

- [ ] **Step 2: Make pain and cycle editorial rather than a repeated card grid**

```css
.problem-statement { color: var(--navy); font-size: clamp(1.5rem, 4vw, 2.6rem); font-weight: 800; }
.recurrence-cycle { display: grid; gap: 1rem; padding: 0; list-style: none; }
```

- [ ] **Step 3: Give product proof and operation sections varied layouts**

Use alternating split sections, a light phone composition, a contrast work-order area, compact operation/WhatsApp paths, and keep price after proof.

```css
.proof-section--maintenance { background: var(--navy); color: var(--white); }
.proof-section--history { background: var(--surface-soft); }
.operation-path, .whatsapp-path { display: flex; flex-wrap: wrap; align-items: center; gap: .6rem; }
```

- [ ] **Step 4: Add desktop rules and retain safeguards**

```css
@media (min-width: 48rem) {
  .proof-grid { grid-template-columns: minmax(0, 1fr) minmax(0, 1fr); align-items: center; }
  .recurrence-cycle { grid-template-columns: repeat(6, minmax(0, 1fr)); }
}
```

Retain `:focus-visible` and `prefers-reduced-motion` behavior.

- [ ] **Step 5: Validate and commit**

```powershell
rg -n 'brand-logo|recurrence-cycle|proof-section|operation-path|whatsapp-path|prefers-reduced-motion' landing/styles.css
git diff --check
git add landing/styles.css
git commit -m "feat: redesign landing visual hierarchy"
```

### Task 3: Configure Render and verify the published artifact

**Files:**

- Create: `render.yaml`
- Create: `deploy/landing/default.conf.template`
- Modify: `deploy/Dockerfile.landing`
- Modify: `landing/landing.js` only if revised markup changes its selectors.

**Interfaces:**

- Consumes: Render `PORT`, Dockerfile path, and the static `landing/` directory.
- Produces: public Render web service `voltamarcada-landing` and an Nginx server bound to `0.0.0.0:$PORT`.

- [ ] **Step 1: Create an Nginx template that uses the runtime port**

```nginx
server {
    listen ${PORT};
    listen [::]:${PORT};
    server_name _;
    root /usr/share/nginx/html;
    index index.html;
    location / { try_files $uri $uri/ =404; }
}
```

- [ ] **Step 2: Update the landing Dockerfile to install the template**

Remove fixed-port `sed` changes, set `ENV PORT=8080`, copy the template to `/etc/nginx/templates/default.conf.template`, retain unprivileged Nginx setup, and expose 8080 for local execution.

```dockerfile
ENV PORT=8080
COPY deploy/landing/default.conf.template /etc/nginx/templates/default.conf.template
```

- [ ] **Step 3: Add a Render Blueprint**

```yaml
services:
  - type: web
    name: voltamarcada-landing
    runtime: docker
    dockerfilePath: deploy/Dockerfile.landing
    dockerContext: .
    healthCheckPath: /
    autoDeployTrigger: commit
```

- [ ] **Step 4: Verify local default and Render-compatible port binding**

```powershell
docker build --file deploy/Dockerfile.landing --tag voltamarcada-landing-redesign .
docker run --rm --publish 8080:8080 voltamarcada-landing-redesign
```

Then open `http://localhost:8080` at 375px and 1440px. Stop it, then run:

```powershell
docker run --rm --env PORT=10000 --publish 10000:10000 voltamarcada-landing-redesign
```

Open `http://localhost:10000`; it must return the landing.

- [ ] **Step 5: Verify graceful enhancement and commit**

Disable JavaScript and confirm content remains visible; emulate reduced motion; block a WebP and confirm its CSS fallback. Then run:

```powershell
git diff --check
git status --short
git add render.yaml deploy/Dockerfile.landing deploy/landing/default.conf.template landing/index.html landing/styles.css landing/landing.js landing/assets/README.md landing/assets/logo.png
git commit -m "feat: deploy landing to render"
```

## Plan Self-Review

- Tasks cover approved recurrence-first copy, price/trial preservation, logo replacement, responsive visual design, screenshot fallbacks, progressive enhancement, and Render access.
- New markup classes are produced in Task 1 and styled in Task 2.
- Render binds to its `PORT` environment value as required for a public web service.
