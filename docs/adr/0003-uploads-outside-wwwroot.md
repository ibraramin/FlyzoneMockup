# ADR-0003: Store uploads outside `wwwroot`, serve them only through an owner-gated action

Status: Accepted (2026-11-27)
Owner: [owner: orchestrator]

## Context
The original code wrote user documents under `/uploads/<guid>` with no auth gate and bound a caller-supplied `applicationId` with no ownership check (IDOR). Static files under the web root bypass authorization entirely.

## Decision
Write uploads to `Uploads:Path` (default `App_Data/uploads`, outside `wwwroot`), generate GUID filenames, validate extension + declared MIME + magic bytes, enforce a 5 MB cap, and serve files only via `Services/DownloadDocument`, which filters by `UserId`. The stored path is reduced to `Path.GetFileName` and resolved inside the configured directory before serving.

## Alternatives considered
- **Keep static serving under `wwwroot` with a rewritten middleware gate**: more moving parts and easy to misconfigure; rejected.
- **Cloud blob storage**: deferred (RESEARCH Tier 2) — adds secrets and external dependencies for no verifiable demo payoff.

## Consequences
Direct static access is impossible; every download is authorized and auditable via the `UserDocuments` row. The upload form posts a real antiforgery token. What would change this decision: a public multi-user deployment needing scalable blob storage.

## Verification
`dotnet test -c Release --no-build` → `Upload_RejectsAttachingDocumentToAnotherUsersApplication` and magic-byte tests pass; code inspection shows no upload directory under `wwwroot`; `MyDocuments` download links resolve to the authorized action.
