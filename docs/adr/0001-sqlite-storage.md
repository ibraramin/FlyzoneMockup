# ADR-0001: Keep SQLite as the demo datastore

Status: Accepted (2026-11-27)
Owner: [owner: orchestrator]

## Context
The repo already had SQLite-typed EF Core migrations and a clean-clone startup path with no extra containers. A NuGet advisory (NU1903, `SQLitePCLRaw.lib.e_sqlite3 2.1.11`) was resolved in P1 by pinning a patched transitive version. A containerized Postgres was proposed as a more "real" backend signal for a portfolio reviewer.

## Decision
Keep SQLite. Document Postgres as an optional profile only, not a default.

## Alternatives considered
- **Postgres profile**: clears any SQLite perception issue but adds a second container, readiness ordering, credentials, and a migration rewrite. Rejected for a self-contained demo.
- **Stay on the unpatched SQLite package**: rejected because NU1903 was a high-severity advisory.

## Consequences
`docker compose up --build` needs only the app container. Multi-user production deployment is explicitly out of scope and stated in the README. What would change this decision: a public multi-user deployment, or a target role requiring multi-service Docker/K8s.

## Verification
`dotnet list package --vulnerable --include-transitive` → no vulnerable packages; `docker compose up --build` from clean volumes → healthy container; `curl -f http://localhost:5000/` → 200.
