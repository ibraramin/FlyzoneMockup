# ADR-0002: Stay server-rendered MVC, no SPA or HTMX

Status: Accepted (2026-11-27)
Owner: [owner: orchestrator]

## Context
The UI is Razor + CDN Bootstrap/jQuery with no JavaScript build step and no JS tests. Prior sessions had already found CSRF and auth gaps around the upload endpoint. HTMX or an SPA would add a second toolchain, partial endpoints, and a new antiforgery surface.

## Decision
Keep server-rendered MVC with tag-helper forms; use plain static JS files only for map/dashboard behavior. No HTMX, no SPA framework.

## Alternatives considered
- **HTMX partial endpoints**: attractive for a "live" timeline but adds partial-view contracts and token handling where gaps already existed.
- **SPA front end**: rejected as disproportionate to a backend-skill portfolio demo.

## Consequences
Every affordance is a normal GET/POST route, so `curl` can verify it. CSP forbids inline/eval scripts; page scripts live in `wwwroot/js/`. What would change this decision: a genuinely live timeline requirement or a front-end-heavy target role.

## Verification
`grep -rn 'href="#"' Views --include=*.cshtml` → no matches; `curl` smoke of every newly linked route → 200; CSP header contains no `unsafe-inline`/`unsafe-eval` in `script-src`.
