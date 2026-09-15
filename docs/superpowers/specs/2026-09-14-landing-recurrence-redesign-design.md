# Landing Page Recurrence Redesign

## Objective

Refactor the VoltaMarcada landing page to lead with its core commercial
differentiator: turning completed maintenance services into future return
opportunities. The page must speak primarily to Brazilian HVAC, climate
control, refrigeration, technical assistance, and equipment-maintenance
companies.

The existing price and trial offer remain unchanged:

- R$ 49,90 per month.
- 14 free days.
- No credit card required.

## Narrative

The page tells one continuous story:

1. A company completes a service, but the customer may disappear into a
   WhatsApp conversation, calendar, spreadsheet, or memory.
2. VoltaMarcada records the recommended next maintenance and surfaces the
   correct customer, equipment, service, and timing.
3. The company contacts the customer, typically through WhatsApp, and turns
   the return into a new request or service.
4. Customer, equipment, scheduling, quotes, and work orders support that
   recurring-service cycle.

The page must not position VoltaMarcada as a generic work-order, scheduling,
or CRM system.

## Page Structure

### Header

Keep the compact sticky navigation, with anchors for the explanatory sections,
pricing, login, and the free-trial CTA.

### Hero

Use the headline “Seu cliente vai precisar de manutenção novamente.” and the
supporting statement that VoltaMarcada helps the company not forget that
customer. Keep the primary free-trial CTA and a secondary in-page “Ver como
funciona” action.

Use the product dashboard as the hero visual. The markup must keep a faithful
CSS fallback only until a real synthetic-data screenshot named
`hero-dashboard.webp` is supplied.

### Problem

Use a spacious, text-led section headed “Quantos clientes você já atendeu e
nunca chamou novamente?” Explain the operational loss without invented
statistics. Make “Serviço concluído ≠ relacionamento encerrado” the visual
conclusion.

### Recurrence Cycle

Make the recurring-service cycle the strongest visual explanation after the
hero:

`Atendimento → Ordem de serviço → Serviço concluído → Próxima manutenção → Contato com o cliente → Novo atendimento`

It must be responsive, readable on small screens, and distinct from a generic
feature-card grid.

### Product Proof

Present the product through varied compositions rather than repeated cards:

- Maintenance panel: who is overdue, due soon, future, contacted, and booked.
- Equipment history: prior services and technical context before a return
  visit.
- Public request experience: a mobile device composition that makes clear the
  customer submits a preferred time and the company confirms it.
- Work order: office-to-field execution, including checklist, photos,
  attachments, observations, and completion.

Each area leads with a plain-language benefit and uses minimal accompanying
copy. Slots must use actual WebP captures when present and preserve clear CSS
fallbacks otherwise.

### Complete Operation

After the recurrence proof, show the supporting operational flow:

`Solicitação → Agenda → Orçamento → Aprovação → Ordem de serviço → Execução → Histórico → Próxima manutenção`

Use a compact set of five supporting modules: clients and equipment,
scheduling, quotes, work orders, and upcoming maintenance. This section
supports the central proposition; it must not overpower it.

### WhatsApp

Explain that VoltaMarcada prepares the customer context and safe link while
the company continues speaking through WhatsApp. Do not promise official API
automation.

### Price, FAQ, and Final CTA

Keep the existing pricing terms and FAQ. The final CTA returns to the central
message: “O próximo serviço pode estar em um cliente que você já atendeu.”
Close with “O serviço termina. O cliente não.”

## Visual Direction

- Retain the VoltaMarcada blue, light blue, white, and neutral palette.
- Increase whitespace and reduce the amount of repeated bordered cards.
- Use strong typography, restrained borders, and light shadows.
- Keep gradients subtle and functional, never a dominant visual device.
- Do not use generic stock imagery, AI imagery, fictional product dashboards,
  unsupported metrics, or excessive animation.
- Prefer real product captures with synthetic data. Images below the first
  viewport must be lazy-loaded and include explicit dimensions.

## Technical Design

The work remains a static landing implementation in these files:

- `landing/index.html` for semantic page structure, copy, metadata, image
  slots, and accessible navigation.
- `landing/styles.css` for responsive layout, visual hierarchy, screenshot
  frames, motion, and reduced-motion support.
- `landing/landing.js` only for the mobile navigation and progressive reveal
  behavior; no new runtime dependency is needed.

Existing CSS visual demonstrations remain isolated inside media slots so they
can be replaced by screenshots without changing section structure. The page
must work without JavaScript, retain the skip link, keyboard focus styles, and
semantic heading order.

## Error Handling and Validation

This static page has no form processing. Its relevant safeguards are:

- Valid, responsive layout when a screenshot asset is absent.
- Non-blocking behavior when JavaScript is disabled or IntersectionObserver is
  unavailable.
- Accessible navigation and focus feedback.
- Correct external application URLs for registration and login.

## Verification

- Inspect the changed HTML, CSS, and JavaScript for encoding, broken anchors,
  and semantic structure.
- Serve or build the landing through its established Docker/preview workflow
  and inspect desktop and mobile rendering.
- Confirm navigation behavior, reduced-motion fallback, screenshot fallback,
  and primary registration links.
