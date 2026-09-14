# Replaceable landing visuals

The first landing release uses CSS demonstration frames so it does not depend on product screenshots. Replace those isolated media slots with the following optimized WebP captures when they are available. Every capture must use synthetic data only; never place real customer, equipment, appointment, contact, or financial data in a marketing asset.

- `hero-dashboard.webp` — 1600x1000 — populated dashboard
- `maintenance-reminders.webp` — 1440x960 — maintenance dashboard
- `public-request-mobile.webp` — 750x1334 — public request page
- `schedule.webp` — 1440x960 — populated schedule
- `work-order.webp` — 1440x960 — filled work order
- `quote.webp` — 1440x960 — optional filled quote

Keep the crop focused on the product, include explicit dimensions in the eventual `<img>` tags, and lazy-load images outside the first viewport.
