# ADR-0004: Cut the minimal admin review queue (conditional, not permanent)

Status: Accepted for P5-A–P5-C (2026-11-27); revisit on publication decision
Owner: [owner: orchestrator]

## Context
`ApplicationHistory` rows are written on submission but no role or transition path exists to append later states, so status history can never advance beyond "Submitted" in the demo. A minimal role-gated admin queue was ranked conditional in `docs/RESEARCH.md` §1/#4: build it only if the repository is published to a technical audience.

## Decision
Do not build admin roles or demo-data seeding. Keep the applicant-side slice coherent (apply → upload → track → read timeline) and keep the README honest that no officer can advance statuses.

## Alternatives considered
- **Build admin + seed fake users/applications**: would make transitions visible but requires the unresolved publication/audience decision and a demo-data policy. Deferred.
- **Seed fake history rows instead of an admin actor**: rejected as dishonest — it would fabricate workflow evidence.

## Consequences
The README's "status history" claim is true as a *read* surface; the claim that statuses can be changed is explicitly not made. A single real submission demonstrates the timeline. What would change this decision: written publication permission plus a technical audience — then build the role-gated review queue before further packaging.

## Verification
`grep -n 'AddRole\|RoleManager' Program.cs` → no matches; README known-limitations states no admin role; a real submission in the smoke DB renders a timeline on Details and Track pages.
