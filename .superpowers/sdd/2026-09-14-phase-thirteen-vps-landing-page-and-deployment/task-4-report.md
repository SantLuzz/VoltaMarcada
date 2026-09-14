# Task 4 — Public landing page report

## Delivered scope

Created the independent, framework-free static landing page under `landing/`:

- `index.html` contains the approved Portuguese narrative, sticky navigation,
  semantic landmarks, one `h1`, public booking preview, recurrence flow,
  feature cards, price, FAQ, final CTA, and footer.
- `styles.css` is mobile-first and uses the VoltaMarcada blue, light-blue,
  white, and navy palette. It includes responsive browser/mobile mockup
  frames, visible keyboard focus, and a reduced-motion override.
- `landing.js` provides only the accessible mobile menu and optional reveal
  behavior when `IntersectionObserver` exists.
- `assets/README.md` documents the six replaceable WebP captures and the
  mandatory synthetic-data-only rule. `favicon.svg` and `og-image.svg` are
  included as static brand assets.
- `LandingPageSourceTests.cs` covers the primary source contracts: exact hero,
  single `h1`, landmarks and section anchors, registration/login destinations,
  price/trial, native FAQ details, metadata, no development hostnames, and
  reduced-motion CSS.

All account-creation calls to action use
`https://app.voltamarcada.com.br/account/register`; existing-account links use
`https://app.voltamarcada.com.br/account/login`.

## TDD record

1. Added the landing source-contract test before creating landing assets.
2. Ran the focused test and observed the expected red state: three failures
   because `landing/index.html` and `landing/styles.css` did not yet exist.
3. Added the static landing implementation.
4. The first green attempt exposed a source-contract mismatch: the displayed
   price was split across HTML elements, so no accessible contiguous
   `R$ 49,90` value was available. Added `aria-label="R$ 49,90 por mês"` to
   the price element.
5. Re-ran the focused test successfully.

## Verification evidence

Executed in the Phase 13 landing worktree:

```text
dotnet test tests\VoltaMarcada.IntegrationTests --no-restore --filter FullyQualifiedName~LandingPageSourceTests --logger "console;verbosity=normal"
```

Result: 3 passed, 0 failed.

```text
dotnet build VoltaMarcada.sln --no-restore
```

Result: succeeded with 0 warnings and 0 errors.

Also ran a source requirement scan and `git diff --check`; no tracked-diff
whitespace issue was reported. The scan confirmed the required destinations,
copy, media replacement names, and reduced-motion rule. (The files were
untracked at that moment, so the focused test is the authoritative source
contract verification.)

## Self-review

- No frontend framework, analytics, tracking code, external font, or image
  dependency was introduced.
- The page uses `lang="pt-BR"`, UTF-8, viewport, exact approved description,
  canonical URL, Open Graph data, and favicon.
- The hamburger button exposes and updates `aria-expanded`, targets
  `aria-controls`, and closes after internal navigation.
- The public request is represented as a preference that requires company
  confirmation; no professional is exposed in the public mockup.
- Demonstrative frames contain only invented/synthetic content. Production
  screenshots remain intentionally replaceable through the documented slots.

## Constraint / concern

Docker is unavailable in this environment. Therefore the requested landing
image build and HTTP container smoke test were not run and are not claimed as
verified. The focused xUnit source test and solution build were run locally.

## Review round 1 follow-up

Addressed the landing review findings in a second TDD cycle:

- Reveal elements are visible by default. Animation-only opacity and transforms
  are now scoped to `.js [data-reveal]`, and `landing.js` enables that class.
  Visitors without JavaScript retain all content.
- The three mockup-only controls are now inert `span.mock-button` elements.
  The mobile navigation toggle is the only HTML button. Browser, phone, and
  reminder mockups are consistently `aria-hidden="true"`; the gallery wrapper
  uses the valid `role="group"` for its accessible label.
- Added six isolated `<picture>/<img>` slots for the documented screenshot
  names, with explicit dimensions. The hero is eager and high priority; the
  five below-fold images are lazy. An image load error hides its picture and
  exposes the adjacent readable CSS mockup. CSS explicitly preserves the
  `hidden` fallback state.
- Added `LandingPageReviewSourceTests` and expanded it to cover progressive
  enhancement, the single real button, decorative accessibility treatment,
  all six media filenames, loading priority, explicit hero dimensions, and
  the safe missing-image fallback.

TDD evidence: the new review contract initially failed for the missing JS
reveal gate, decorative buttons, and absent media slots. A later focused run
also failed for the missing CSS hidden-image rule; after adding that rule, all
six focused landing source tests passed.