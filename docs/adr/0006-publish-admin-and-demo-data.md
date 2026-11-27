# ADR-0006: Publish the repo, add a minimal Admin role, and seed opt-in demo data

Status: Accepted (2026-11-27)
Owner: [owner: orchestrator]
Supersedes: ADR-0004 (the "no admin this burst" decision)

## Context
ADR-0004 cut the admin queue and demo seeding because publication and audience were undecided. The founder has now resolved both: publishing means a GitHub push with a generic MIT license and a README aimed at viewers such as potential employers; admin roles are explicitly wanted; demo-data policy is delegated to the orchestrator.

## Decision
- Add ASP.NET Core Identity roles and an `Admin` role. `AdminController` is `[Authorize(Roles = "Admin")]` and offers a filterable review queue plus a transactional status/history transition with internal notes.
- Add an opt-in `DemoDataSeeder` gated by `DemoData:Enabled` (false in `appsettings.json`, true in `appsettings.Development.json` and Compose). It creates public demo applicant/admin accounts and three applications with realistic status history. Tests explicitly disable it.
- Publish under MIT (`LICENSE`), with demo credentials and a CI badge in the README, and push the single local commit to `origin/main`.

## Alternatives considered
- **No seeded admin; promote a user manually**: rejected — viewers would not see the workflow without setup.
- **Always-on demo accounts**: rejected — a published app should not create privileged accounts by default; the flag makes the risk explicit and testable.
- **Keep the repository private**: rejected by the founder decision.

## Consequences
The README's status-history claim is now fully demonstrable: an admin transitions a sample application and the applicant timeline updates. Security regression tests cover role gating, antiforgery on transitions, atomic history append and non-admin denial. Demo credentials are intentionally public but gated; production deployments must leave `DemoData:Enabled=false`. What would change this decision: a real multi-user deployment requiring per-user authorization rules, approval chains or private accounts.

## Verification
`dotnet test -c Release --no-build` → 27 passed (5 new admin tests, including unknown-status rejection); `curl` on `/Admin` as demo admin → 200, as applicant → redirect to access denied; Compose smoke → healthy container, seeded demo accounts available; `git status --porcelain` clean before push.
