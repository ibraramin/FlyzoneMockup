# ADR-0005: Render the service catalog from `TypingServices`, resolve form service ids by name

Status: Accepted (2026-11-27)
Owner: [owner: orchestrator]

## Context
The landing page contained seven hard-coded "Service 1..5" / lorem-ipsum cards while the database had a `TypingServices` table with only two seeded rows and an unused `IsActive` flag. Two application forms hard-coded `ServiceId = 1` and `ServiceId = 2`.

## Decision
`HomeController.Index` queries active catalog rows (`IsActive`, ordered by `Id`) and the Razor view renders category, name, description and base fee from the entity. Seed data is expanded to six active rows through an EF migration (`SeedServiceCatalog`). Forms resolve their `ServiceId` from the catalog by exact service name and throw if the row is missing or inactive.

## Alternatives considered
- **Keep static cards and just add more seed rows**: rejected — the cards would still lie about being data-driven.
- **Keep integer literals with a comment**: rejected — re-seeding or re-ordering the catalog could attach the wrong service silently.

## Consequences
Changing the public catalog requires only a DB row change (plus a migration when seed data changes). Fee formatting is pinned to `InvariantCulture` so rendering and tests are locale-independent. What would change this decision: a CMS or admin surface for catalog editing (not in scope).

## Verification
`curl` on `/` → real service names and fees, no filler copy; `dotnet test -c Release --no-build` → `LandingPage_HidesInactiveCatalogRows` and `GoldenVisaSubmission_UsesCatalogServiceId_NotHardcodedInteger` (which re-keys the catalog row to id 101) pass.
